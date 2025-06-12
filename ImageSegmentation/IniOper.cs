using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageSegmentation
{
    internal class IniFile
    {

        private string m_sFilePath;
        private StringBuilder m_sbValue;
        private int m_nReadBuf;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileInt(string section, string key, int def, string fileName);

        public IniFile(string sFilePath)
        {
            m_sFilePath = sFilePath;
            m_nReadBuf = 512;
            m_sbValue = new StringBuilder(m_nReadBuf);
        }

        // 讀取 INI File Key Value
        public string ReadValue(string sSection, string sKey)
        {
            m_sbValue.Remove(0, m_sbValue.Length);
            GetPrivateProfileString(sSection, sKey, "", m_sbValue, m_nReadBuf, m_sFilePath);
            return m_sbValue.ToString();
        }
        public string ReadString(string sSection, string sKey, string sDef)
        {
            m_sbValue.Remove(0, m_sbValue.Length);
            GetPrivateProfileString(sSection, sKey, sDef, m_sbValue, m_nReadBuf, m_sFilePath);
            return m_sbValue.ToString();
        }
        public int ReadInt(string sSection, string sKey, int nDef)
        {
            int i = GetPrivateProfileInt(sSection, sKey, nDef, m_sFilePath);
            return i;
        }
        public bool ReadBool(string sSection, string sKey, bool bDef)
        {
            string sDef2;
            if (bDef) sDef2 = "True"; else sDef2 = "False";
            m_sbValue.Remove(0, m_sbValue.Length);
            GetPrivateProfileString(sSection, sKey, sDef2, m_sbValue, m_nReadBuf, m_sFilePath);
            if (m_sbValue.ToString() == "True") return true;
            else return false;
        }
        public Double ReadDouble(string sSection, string sKey, double dDef)
        {
            string sDef = dDef.ToString();
            return Convert.ToDouble(ReadString(sSection, sKey, sDef));
        }
        public float ReadFloat(string sSection, string sKey, float fDef)
        {
            string sDef = fDef.ToString();
            return float.Parse(ReadString(sSection, sKey, sDef));
        }

        public string ReadUser(string section, string key)
        {
            var retVal = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", retVal, 255, m_sFilePath);
            return retVal.ToString();
        }

        public string Read(string section, string key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, "", temp, 255, m_sFilePath);
            return temp.ToString();
        }

        public void Write(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, m_sFilePath);
        }
        public string Read(string section, string key, string def)
        {
            StringBuilder retVal = new StringBuilder(255);
            GetPrivateProfileString(section, key, def, retVal, 255, m_sFilePath);
            return retVal.ToString();
        }



        // 將值寫入 INI File
        public void WriteValue(string sSection, string sKey, StringBuilder retVal, string sValue)
        {
            WritePrivateProfileString(sSection, sKey, sValue, m_sFilePath);
        }

        public void WriteValue(string sSection, string sKey, StringBuilder retVal, int nValue)
        {
            WritePrivateProfileString(sSection, sKey, nValue.ToString(), m_sFilePath);
        }

        public void WriteValue(string sSection, string sKey, float fValue)
        {
            WritePrivateProfileString(sSection, sKey, fValue.ToString(), m_sFilePath);
        }

        public void WriteValue(string sSection, string sKey, double fValue)
        {
            WritePrivateProfileString(sSection, sKey, fValue.ToString(), m_sFilePath);
        }

        public void WriteValue(string sSection, string sKey, bool bValue)
        {
            WritePrivateProfileString(sSection, sKey, bValue.ToString(), m_sFilePath);
        }

        public void WriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, m_sFilePath);
        }
    }
}
    

