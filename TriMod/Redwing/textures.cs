using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace TriMod.Redwing
{
    public static class textures
    {
        public static void loadAllTextures()
        {
            foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                
                if (res.Contains(BeamPrefix))
                {
                    for (int i = 0; i < FocusBeam.Length; i++)
                    {
                        if (res.EndsWith(i + ".png"))
                        {
                            FocusBeam[i] = loadImageFromAssembly(res);
                        }
                    }
                } else if (res.Contains(WarpPrefix))
                {
                    for (int i = 0; i < WarpSprites.Length; i++)
                    {
                        WarpSprites[i] = loadImageFromAssembly(res);
                    }
                }

                if (res.EndsWith("512_512.png"))
                {
                    _invalidSquare = loadImageFromAssembly(res);
                } else if (res.EndsWith("512_256.png"))
                {
                    _invalidShort = loadImageFromAssembly(res);
                } else if (res.EndsWith("256_512.png"))
                {
                    _invalidTall = loadImageFromAssembly(res);
                } else if (res.EndsWith("256_1024.png"))
                {
                    _invalidVeryTall = loadImageFromAssembly(res);
                }
                Log("Found resource with name " + res);
            }
            invalidateNullTextures();
        }

        private static void invalidateNullTextures()
        {
            // First check all our invalids are not null
            if (_invalidShort == null || _invalidSquare == null || _invalidTall == null || _invalidVeryTall == null)
            {
                throw new NullReferenceException("Our 'missing texture' textures are null and thus we cannot set any missing textures to them." +
                                                 "\nEnsure the game is compiled with them.");
            }

            for (int i = 0; i < FocusBeam.Length; i++)
            {
                //Log("Isnull? " + (FocusBeam[i] == null) + " height " + FocusBeam[i].height + " width " + FocusBeam[i].width );
                if (FocusBeam[i] == null )
                {
                    Log("No focus beam texture for focus beam " + i);
                    FocusBeam[i] = _invalidTall;
                }
            }

            for (int i = 0; i < WarpSprites.Length; i++)
            {
                if (WarpSprites[i] == null )
                {
                    Log("No warp sprite texture for warp" + i + ".png");
                    WarpSprites[i] = _invalidSquare;
                }
            }
        }
        
        private static Texture2D loadImageFromAssembly(string imageName)
        {
            //Create texture from bytes
            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(getBytes(imageName));
            return tex;
        }
        
        private static byte[] getBytes(string filename){
            Stream dataStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filename);
            if (dataStream == null) return null;
            
            byte[] buffer = new byte[dataStream.Length];
            dataStream.Read(buffer, 0, buffer.Length);
            dataStream.Dispose();
            return buffer;
        }
        
        public static readonly Texture2D[] FocusBeam = new Texture2D[4];
        public static readonly Texture2D[] WarpSprites = new Texture2D[8];
        private static Texture2D _invalidSquare;
        private static Texture2D _invalidTall;
        private static Texture2D _invalidShort;
        private static Texture2D _invalidVeryTall;
        private const string BeamPrefix = "focusbeam";
        private const string WarpPrefix = "warp";
        //public const int flameLeftX = 200;
        
        private static void Log(string message)
        {
            Modding.Logger.Log("[Trimod:Redwing] : " + message);
        }
    }
}