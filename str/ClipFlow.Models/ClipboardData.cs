using System.Collections.Specialized;

namespace ClipFlow.Models
{

    public class ClipboardData
    {
        public StringCollection CopyFiles { get; set; }
        public string Text { get; set; }
        public ClipboardType Type { get; set; }
        public string Uuid { get; set; }
        public string Description { get; set; }
        public ulong? DataLength { get; set; }
        public string FileName { get; set; }
        /// <summary>
        /// 进程名
        /// </summary>
        public string ProcessName { get; set; }
    }

    public enum ClipboardType
    {
        Text,       // 文本
        File,      // 单个文件
        FileList,   // 多个文件
    }
}