using System;

namespace ClipFlow.Models
{
    /// <summary>
    /// 剪贴板数据类
    /// </summary>
    public class ClipboardData
    {
        // 文本内容
        public string Text { get; set; }
        // 二进制内容
        public byte[] Data { get; set; }
        //大小
        public ulong? DataLength { get; set; }
        // 内容类型
        public ClipboardType Type { get; set; }
        // 描述信息
        public string Description { get; set; }

        // 上传和下载文件名（非文本类型使用）
        public string Filename { get; set; }
        public string Uuid { get; set; }
        //Clipboard文件集合
        public List<string> FilenameList { get; set; }

    }

    public enum FileType
    {
        Image,      // 图片
        Document, //文档
        Archive,  //压缩文件
        Media, //媒体
        Other       // 其他
    }

    public enum ClipboardType
    {
        Text,       // 文本
        File,      // 单个文件
        FileList,   // 多个文件
    }
}