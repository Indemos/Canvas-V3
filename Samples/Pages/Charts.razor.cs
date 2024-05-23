using Canvas.Core.Engines;
using Canvas.Core.Models;
using Canvas.Core.Shapes;
using Canvas.Views.Web.Views;
using Distribution.Collections;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Canvas.Client.Pages
{
  public partial class Charts
  {
    protected CanvasGroupView View { get; set; }
    protected Random Generator { get; set; } = new();
    protected DateTime Time { get; set; } = DateTime.Now;
    protected DateTime TimeGroup { get; set; } = DateTime.Now;
    protected ObservableGroupCollection<IShape> Points { get; set; } = new();

    protected override async Task OnAfterRenderAsync(bool setup)
    {
      if (setup)
      {
        View.Item = GetSample();

        var interval = new Timer(TimeSpan.FromMicroseconds(1));
        var views = await View.CreateViews<CanvasEngine>();

        views.ForEach(o => o.ShowIndex = v => GetDateByIndex(o.Items, (int)v));

        interval.Enabled = true;
        interval.Elapsed += (o, e) =>
        {
          if (Points.Count >= 100)
          {
            interval.Stop();
          }

          OnData();
        };
      }

      await base.OnAfterRenderAsync(setup);
    }

    protected GroupShape GetSample()
    {
      static GroupShape GetGroup() => new() { Groups = new Dictionary<string, IShape>() };

      var group = GetGroup();

      group.Groups["Assets"] = GetGroup();
      group.Groups["Assets"].Groups["Prices"] = new CandleShape();
      group.Groups["Assets"].Groups["Arrows"] = new ArrowShape();

      group.Groups["Indicators"] = GetGroup();
      group.Groups["Indicators"].Groups["Bars"] = new BarShape();

      group.Groups["Lines"] = GetGroup();
      group.Groups["Lines"].Groups["X"] = new LineShape();
      group.Groups["Lines"].Groups["Y"] = new LineShape();

      group.Groups["Performance"] = GetGroup();
      group.Groups["Performance"].Groups["Balance"] = new AreaShape();

      return group;
    }

    protected string GetDateByIndex(IList<IShape> items, int index)
    {
      var empty = index <= 0 ? items.FirstOrDefault()?.X : items.LastOrDefault()?.X;
      var stamp = (long)(items.ElementAtOrDefault(index)?.X ?? empty ?? DateTime.Now.Ticks);

      return $"{new DateTime(stamp):MM/dd/yyyy HH:mm}";
    }

    /// <summary>
    /// On timer event
    /// </summary>
    protected void OnData()
    {
      var sample = GetSample();
      var min = Generator.Next(1000, 2000);
      var max = Generator.Next(3000, 5000);
      var point = Generator.Next(min, max);
      var duration = TimeSpan.FromSeconds(5);
      var candleColor = point % 2 is 0 ? SKColors.LimeGreen : SKColors.OrangeRed;
      var barColor = point % 2 is 0 ? SKColors.DeepSkyBlue : SKColors.OrangeRed;
      var color = SKColors.DeepSkyBlue;
      var direction = point % 2 is 0 ? 1 : -1;

      Time += TimeSpan.FromMinutes(1);
      TimeGroup = Time - TimeGroup > TimeSpan.FromMinutes(10) ? Time : TimeGroup;

      sample.X = TimeGroup.Ticks;
      sample.Groups["Lines"].Groups["X"] = new LineShape { Y = point + max };
      sample.Groups["Lines"].Groups["Y"] = new LineShape { Y = point - min };
      sample.Groups["Indicators"].Groups["Bars"] = new BarShape { Y = point, Component = new ComponentModel { Color = barColor } };
      sample.Groups["Performance"].Groups["Balance"] = new AreaShape { Y = point, Component = new ComponentModel { Color = SKColors.DeepSkyBlue } };
      sample.Groups["Assets"].Groups["Arrows"] = new ArrowShape { Y = point, Direction = direction };
      sample.Groups["Assets"].Groups["Prices"] = new CandleShape { Y = point, Component = new ComponentModel { Color = candleColor } };

      Points.Add(sample, true);

      var domain = new DomainModel { IndexDomain = new int[] { Points.Count - 100, Points.Count } };

      View.Update(domain, Points);
    }
  }
}
