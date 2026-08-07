using System;
using System.Collections.Generic;

namespace Fragments.Data
{
    [Serializable]
    public class JournalData
    {
        public string id;
        public string journalName;
        public string coverColorHex;
        public string pagePattern; // "plain", "dotted", "striped"
        public long createdAt;
        public long lastOpenedAt;
        public List<PageData> pages = new();
    }

    [Serializable]
    public class PageData
    {
        public int pageIndex;
        public string backgroundColorHex;
        public List<FragmentData> fragments = new();
    }

    [Serializable]
    public class FragmentData
    {
        public string kind;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ;
        public float scaleX, scaleY;
        public float opacity;
        public string texturePath;
        public string sourceName;
        public string sourceDesc;
        public string capturedAt;
    }
}