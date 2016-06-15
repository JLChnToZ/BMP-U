using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;

namespace BMS {
    public partial class BMSManager: MonoBehaviour {
        string[] bmsContent;
        bool bmsLoaded = false;

        public bool BMSLoaded {
            get { return bmsLoaded; }
        }

        public event Action OnBMSLoaded;

        public void LoadBMS(string bmsContent, string resourcePath, bool direct = false) {
            StopPreviousBMSLoadJob();
            var bmsContentList = new List<string>();
            foreach(var line in Regex.Split(bmsContent, "\r\n|\r|\n"))
                if(!string.IsNullOrEmpty(line) && line[0] == '#')
                    bmsContentList.Add(line);
            this.bmsContent = bmsContentList.ToArray();
            this.resourcePath = resourcePath;
            bmsLoaded = false;
            ClearDataObjects(true, direct);
            ReloadBMS(BMSReloadOperation.Header, direct);
        }

        public void ReloadBMS(BMSReloadOperation reloadType, bool direct = false) {
            bool header = (reloadType & BMSReloadOperation.Header) == BMSReloadOperation.Header;
            bool body = (reloadType & BMSReloadOperation.Body) == BMSReloadOperation.Body;
            bool res = (reloadType & BMSReloadOperation.Resources) == BMSReloadOperation.Resources;
            bool resHeader = (reloadType & BMSReloadOperation.ResourceHeader) == BMSReloadOperation.ResourceHeader;
            if(header || body) {
                if(res && !resHeader)
                    ClearDataObjects(true, direct);
                ReloadTimeline(header, body, resHeader, direct);
            } else if(res)
                ClearDataObjects(false, direct);
            if(res)
                ReloadResources();
        }
    }
}
