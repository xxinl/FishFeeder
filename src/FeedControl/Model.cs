using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FeedControl
{
  public class Setting
  {
    public int Id { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
  }

  public class FeedLog
  {
    public int Id { get; set; }
    public DateTime EntryTime { get; set; }
    public FeedLogType Type { get; set; }
    public string Content { get; set; }
  }

  public enum FeedLogType
  {
    FeedDone = 0,
    PingLog = 1
  }
}
