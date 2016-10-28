using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace FeedControl.Controllers
{
  [Route("api")]
  public class ApiController : Controller
  {
    private const string FEED_HOUR_KEY = "FEED_HOUR_KEY";
    private readonly IOptions<FeedConfig> config;

    public ApiController(IOptions<FeedConfig> config)
    {
      this.config = config;
    }

    [Route("oktofeed"), HttpGet]
    public bool CheckOkToFeed()
    {
      using (var db = new DataContext())
      {
        var hourStr = db.Settings.First(s => s.Key == "FEED_HOUR_KEY");
        var feedHour = Convert.ToInt32(hourStr);

        var now = DateTime.Now;
        var lastFeedTime = db.FeedLogs.OrderByDescending(l => l.EntryTime).FirstOrDefault(l => l.Type == FeedLogType.FeedDone);
        if (lastFeedTime != null && lastFeedTime.EntryTime.Day < now.Day && feedHour == now.Hour)
        {
          return true;
        }
        else
        {
          //TODO log ping
          return false;
        }
      }
    }

    [Route("getfeedlogs"), HttpGet]
    public IEnumerable<FeedLog> GetFeedLogs()
    {
      using (var db = new DataContext())
      {
        return db.FeedLogs.OrderByDescending(l => l.EntryTime);
      }
    }
  }
}
