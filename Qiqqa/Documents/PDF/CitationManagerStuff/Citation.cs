﻿namespace Qiqqa.Documents.PDF.CitationManagerStuff
{
    public class Citation
    {
        public enum Type
        {
            AUTO_CITATION,
            MANUAL_LINK
        }

        public string fingerprint_outbound;
        public string fingerprint_inbound;
        public Type type;

        public override bool Equals(object obj)
        {
            Citation other = obj as Citation;
            
            if (null == other) return false;

            if (this.fingerprint_outbound != other.fingerprint_outbound) return false;
            if (this.fingerprint_inbound != other.fingerprint_inbound) return false;
            if (this.type != other.type) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + this.fingerprint_outbound.GetHashCode();
            hash = hash * 37 + this.fingerprint_inbound.GetHashCode();
            hash = hash * 37 + this.type.GetHashCode();
            return hash;
        }
    }
}
