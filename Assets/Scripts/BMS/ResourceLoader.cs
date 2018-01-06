using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using ManagedBass;

#if UNITY_STANDALONE_WIN
using System.Drawing;
using System.Drawing.Imaging;
#elif UNITY_ANDROID

#else
using Cairo;
using Path = System.IO.Path;
#endif

using UnityEngine;

namespace BMS {
    internal class ResourceLoader {
        static readonly string[] imageTypes = new[] { ".bmp", ".emf", ".gif", ".ico", ".jpg", ".jpe", ".jpeg", ".png", ".tif", ".tiff", ".wmf" };
        static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        readonly Dictionary<string, object> objectCache = new Dictionary<string, object>();

        readonly string basePath;

        public ResourceLoader(string path) {
            basePath = path;
        }

        public IEnumerator LoadResource(ResourceObject resource, Action callback = null) {
            switch(resource.type) {
                case ResourceType.wav: return LoadWavRes(resource, callback);
                case ResourceType.bmp: return LoadBmpRes(resource, callback);
                default: return null;
            }
        }

        FileInfo FindRes(ResourceObject resource, string checkType) {
            var finfo = new FileInfo(Path.Combine(basePath, resource.path));
            if(finfo.Exists)
                return finfo;
            if(!finfo.Directory.Exists)
                return null;
            string path = finfo.Name;
            string extension = finfo.Extension;
            if(extension.Equals(checkType, StringComparison.OrdinalIgnoreCase)) {
                var files = finfo.Directory.GetFiles(path.Substring(0, path.Length - extension.Length) + ".*");
                if(files.Length > 0)
                    return files[0];
            }
            return null;
        }

        IEnumerator LoadWavRes(ResourceObject resource, Action callback) {
            var finfo = FindRes(resource, ".wav");
            if(finfo != null && finfo.Exists) {
                try {
                    /*if(!objectCache.TryGetValue(finfo.FullName, out resource.value)) {
                        resource.value = ReadAudioClipExtended(finfo);
                        objectCache.Add(finfo.FullName, resource.value);
                    }*/
                    resource.value = ReadAudioClipExtended(finfo);
                } catch(Exception ex) {
                    Debug.LogWarningFormat("Exception thrown while loading \"{0}\": {1}\n{2}", finfo.Name, ex.Source, ex.StackTrace);
                }
            } else {
                Debug.LogWarningFormat("Resource {0} not found.", resource.path);
            }
            if(callback != null) callback.Invoke();
            yield break;
        }

        IEnumerator LoadBmpRes(ResourceObject resource, Action callback) {
            var finfo = FindRes(resource, ".bmp");
            if(finfo != null && !objectCache.TryGetValue(finfo.FullName, out resource.value)) {
                bool isImage = false;
                var ext = finfo.Extension.ToLower();
                foreach(var imageType in imageTypes)
                    if(ext == imageType) {
                        isImage = true;
                        break;
                    }
                if(isImage)
                    resource.value = ReadTextureFromFile(finfo);
                else
                    resource.value = ReadMovieTextureFromFile(finfo);

                objectCache.Add(finfo.FullName, resource.value);
            }
            if(callback != null) callback.Invoke();
            yield break;
        }

        static int ReadAudioClipExtended(FileInfo finfo) {
            int handle = Bass.CreateStream(finfo.FullName, 0, 0, BassFlags.Prescan);
            if(handle == 0)
                Debug.LogErrorFormat("Failed to load {0}: {1}", finfo.Name, Bass.LastError);
            return handle;
        }

        #region Platform specific implementations of reading bitmap
#if UNITY_STANDALONE
        static Texture2D ReadTextureFromFile(FileInfo finfo) {
            FileStream fs = null;
            Image img = null;
            Bitmap bmp = null;
            BitmapData bmpData = null;
            Texture2D result = null;
            if(textureCache.TryGetValue(finfo.FullName, out result) && result != null)
                return result;
            try {
                fs = finfo.OpenRead();
                img = Image.FromStream(fs, true);
                bmp = new Bitmap(img);
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                result = new Texture2D(bmp.Width, bmp.Height, TextureFormat.BGRA32, false);
#if UNITY_EDITOR
                result.name = finfo.Name;
#endif
                result.wrapMode = TextureWrapMode.Repeat;
                result.LoadRawTextureData(bmpData.Scan0, Math.Abs(bmpData.Stride) * bmp.Height);
                result.Apply();
                bmp.UnlockBits(bmpData);
                textureCache[finfo.FullName] = result;
                return result;
            } catch {
                return null;
            } finally {
                if(bmp != null) bmp.Dispose();
                if(img != null) img.Dispose();
                if(fs != null) fs.Dispose();
            }
        }

        static MovieTextureHolder ReadMovieTextureFromFile(FileInfo finfo) {
            return MovieTextureHolder.Create(finfo); // Stub
        }
#elif UNITY_ANDROID
        enum BitmapConfig {
            ALPHA_8 = 1,
            RGB_565 = 3,
            ARGB_4444 = 4,
            ARGB_8888 = 5,
        }

        static Texture2D ReadTextureFromFile(FileInfo finfo) {
            using(var bitmapFactory = new AndroidJavaClass("android.graphics.BitmapFactory"))
            using(var bitmapFile = bitmapFactory.CallStatic<AndroidJavaClass>("decodeFile", finfo.FullName)) {
                int width = bitmapFile.Call<int>("getWidth");
                int height = bitmapFile.Call<int>("getHeight");
                int length = width * height;
                var rawArray = new int[length];
                var colorArray = new Color32[length];
                var texture = new Texture2D(width, height, TextureFormat.DXT5, false);
                var bmpConfig = (BitmapConfig)bitmapFile.Call<int>("getConfig");
                bitmapFile.Call("getPixels", rawArray, 0, width, 0, 0, width, height);
                for(int i = 0; i < length; i++)
                    colorArray[i] = ConvertColor(bmpConfig, rawArray[i]);
                texture.SetPixels32(colorArray);
                bitmapFile.Call("recycle");
                return texture;
            }
        }

        static Color32 ConvertColor(BitmapConfig bitmapConfig, int rawValue) {
            unchecked {
                switch(bitmapConfig) {
                    case BitmapConfig.ALPHA_8:
                        return new Color32 {
                            a = (byte)((rawValue & 0xFF)),
                            r = 0xFF,
                            g = 0xFF,
                            b = 0xFF
                        };
                    case BitmapConfig.RGB_565:
                        return new Color32 {
                            a = 0xFF,
                            r = (byte)((rawValue & 0xF800) >> 8),
                            g = (byte)((rawValue & 0x07E0) >> 3),
                            b = (byte)((rawValue & 0x001F) << 3)
                        };
                    case BitmapConfig.ARGB_4444:
                        return new Color32 {
                            a = (byte)((rawValue & 0xF000) >> 8),
                            r = (byte)((rawValue & 0x0F00) >> 4),
                            g = (byte)((rawValue & 0x00F0)),
                            b = (byte)((rawValue & 0x000F) << 4)
                        };
                    case BitmapConfig.ARGB_8888:
                        return new Color32 {
                            a = (byte)((rawValue & 0xFF000000) >> 24),
                            r = (byte)((rawValue & 0x00FF0000) >> 16),
                            g = (byte)((rawValue & 0x0000FF00) >> 8),
                            b = (byte)((rawValue & 0x000000FF))
                        };
                    default: return new Color32();
                }
            }
        }

        static Texture ReadMovieTextureFromFile(FileInfo finfo) {
            return null; // Stub
        }
#else
        static Texture2D ReadTextureFromFile(FileInfo finfo) {
            Debug.LogError("Read texture is not supported in current platfrom.");
            return null;
        }

        static Texture ReadMovieTextureFromFile(FileInfo finfo) {
            Debug.LogError("Read texture is not supported in current platfrom.");
            return null;
        }
#endif
        #endregion
    }
}
