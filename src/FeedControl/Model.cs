using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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

    [NotMapped]
    public List<string> Pics { get; set; }
  }

  public enum FeedLogType
  {
    FeedDone = 0,
    PingLog = 1
  }
}
