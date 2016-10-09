using System;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;

using LitJson;

namespace BMS {
    internal class BMSHashGenerator {
        Encoding encoding;
        HashAlgorithm hashAlgorithm;

        public BMSHashGenerator(Encoding encoding, HashAlgorithm hashAlgorithm) {
            this.hashAlgorithm = hashAlgorithm ?? MD5.Create();
            this.encoding = encoding ?? Encoding.Default;
        }

        public string GetHash(string[] content) {
            return GetHash(string.Join("\n", content));
        }

        public string GetHash(string content) {
            var bytes = encoding.GetBytes(content);
            var hash = hashAlgorithm.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    public partial class BMSManager: MonoBehaviour {
        bool bmsLoaded = false;

        public bool BMSLoaded {
            get { return bmsLoaded; }
        }

        public event Action OnBMSLoaded;

        public void LoadBMS(string bmsContent, string resourcePath, string extension, bool direct = false) {
            BMSFileType bmsFileType;
            switch(extension.ToLower()) {
                case ".bms": bmsFileType = BMSFileType.Standard; break;
                case ".bme": bmsFileType = BMSFileType.Extended; break;
                case ".bml": bmsFileType = BMSFileType.Long; break;
                case ".pms": bmsFileType = BMSFileType.Popn; break;
                case ".bmson": bmsFileType = BMSFileType.Bmson; break;
                default: bmsFileType = BMSFileType.Unknown; break;
            }
            LoadBMS(bmsContent, resourcePath, bmsFileType, direct);
        }

        public void LoadBMS(string bmsContent, string resourcePath, BMSFileType bmsFileType, bool direct = false) {
            StopPreviousBMSLoadJob();
            fileType = bmsFileType;
            switch(bmsFileType) {
                case BMSFileType.Standard:
                case BMSFileType.Extended:
                case BMSFileType.Long:
                case BMSFileType.Popn:
                    chart = new BMSChart(bmsContent);
                    break;
                case BMSFileType.Bmson:
                    chart = new BmsonChart(bmsContent);
                    break;
            }
            if(preTimingHelper != null)
                preTimingHelper.BMSEvent -= OnPreEvent;
            if(mainTimingHelper != null)
                mainTimingHelper.BMSEvent -= OnEventUpdate;
            preTimingHelper = chart.GetEventDispatcher();
            mainTimingHelper = chart.GetEventDispatcher();
            preTimingHelper.BMSEvent += OnPreEvent;
            mainTimingHelper.BMSEvent += OnEventUpdate;
            this.resourcePath = resourcePath;
            bmsLoaded = false;
            ClearDataObjects(true, direct, true);
            ReloadBMS(BMSReloadOperation.Header, direct);
        }

        public void ReloadBMS(BMSReloadOperation reloadType, bool direct = false) {
            bool header = (reloadType & BMSReloadOperation.Header) == BMSReloadOperation.Header;
            bool body = (reloadType & BMSReloadOperation.Body) == BMSReloadOperation.Body;
            bool res = (reloadType & BMSReloadOperation.Resources) == BMSReloadOperation.Resources;
            bool resHeader = (reloadType & BMSReloadOperation.ResourceHeader) == BMSReloadOperation.ResourceHeader;
            if(header || body) {
                if(res && !resHeader)
                    ClearDataObjects(true, direct, header);
                ReloadTimeline(header, body, resHeader, direct);
            } else if(res)
                ClearDataObjects(false, direct, header);
            if(res)
                ReloadResources();
        }

        public string GetHash(Encoding encoding, HashAlgorithm hashAlgorithm) {
            return new BMSHashGenerator(encoding, hashAlgorithm).GetHash(chart.RawContent);
        }
    }
}
