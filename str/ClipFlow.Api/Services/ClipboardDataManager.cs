using ClipFlow.Api.Controllers;
using ClipFlow.Models;
using System.Collections.Concurrent;

namespace ClipFlow.Api.Services
{
    public class ClipboardDataManager
    {
        private readonly ConcurrentDictionary<string, Queue<ClipboardData>> _clipboardHistory = new();
        private const int MaxHistorySize = 3;

        public void AddRecord(string token, ClipboardData record)
        {
            var queue = _clipboardHistory.GetOrAdd(token, _ => new Queue<ClipboardData>());

            // 如果队列已满
            if (queue.Count >= MaxHistorySize)
            {
                // 找到最后一个文本记录
                var lastText = queue.LastOrDefault(x => x.Type == ClipboardType.Text);
                
                // 如果存在文本记录且新记录不是文本类型
                if (lastText != null && record.Type != ClipboardType.Text)
                {
                    // 如果最旧的记录就是最后一个文本记录，不要移除它
                    if (queue.Peek() == lastText)
                    {
                        // 找到第二个最旧的非文本记录并移除它
                        var tempList = queue.ToList();
                        for (int i = 1; i < tempList.Count; i++)
                        {
                            if (tempList[i].Type != ClipboardType.Text)
                            {
                                // 重建队列，跳过要移除的项
                                queue.Clear();
                                foreach (var item in tempList.Where((_, index) => index != i))
                                {
                                    queue.Enqueue(item);
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        // 移除最旧的记录
                        queue.Dequeue();
                    }
                }
                else
                {
                    // 如果新记录是文本类型或没有文本记录，直接移除最旧的记录
                    queue.Dequeue();
                }
            }

            queue.Enqueue(record);
        }

        public Queue<ClipboardData> GetHistory(string token)
        {
            return _clipboardHistory.GetOrAdd(token, _ => new Queue<ClipboardData>());
        }

        public ClipboardData? GetLatest(string token)
        {
            var queue = GetHistory(token);
            return queue.LastOrDefault();
        }

        public ClipboardData? GetLatestText(string token)
        {
            var queue = GetHistory(token);
            return queue.LastOrDefault(x => x.Type == ClipboardType.Text);
        }

        public ClipboardData? GetByUuid(string token, string uuid)
        {
            var queue = GetHistory(token);
            return queue.FirstOrDefault(x => x.Uuid == uuid);
        }

        public IEnumerable<Queue<ClipboardData>> GetAllHistories()
        {
            return _clipboardHistory.Values;
        }
    }
} 