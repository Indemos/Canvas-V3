using Canvas.Core;
using Canvas.Core.EngineSpace;
using Canvas.Core.ModelSpace;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ScriptContainer;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Canvas.Views.Web
{
  public partial class CanvasWebView : IDisposable
  {
    [Inject] protected virtual IJSRuntime RuntimeService { get; set; }

    /// <summary>
    /// Parameters
    /// </summary>
    public virtual Composer Composer { get; set; }
    public virtual Action<ViewMessage> OnUpdate { get; set; } = o => { };

    /// <summary>
    /// Accessors
    /// </summary>
    protected virtual Task Updater { get; set; }
    protected virtual ViewMessage Move { get; set; }
    protected virtual ViewMessage Cursor { get; set; }
    protected virtual StreamServer Server { get; set; }
    protected virtual ScriptMessage Bounds { get; set; }
    protected virtual ScriptService Service { get; set; }
    protected virtual ElementReference ScaleContainer { get; set; }
    protected virtual ElementReference CanvasContainer { get; set; }

    /// <summary>
    /// Enumerate indices
    /// </summary>
    public virtual IEnumerable<IPointModel> GetIndexEnumerator()
    {
      if (Composer?.Engine is not null)
      {
        var cnt = (double)Composer.IndexLabelCount + 1.0;
        var step = (double)Composer.Engine.IndexSize / cnt;
        var stepValue = (double)(Composer.MaxIndex - Composer.MinIndex) / cnt;

        for (var i = 1.0; i < cnt; i++)
        {
          yield return new PointModel
          {
            Index = step * i,
            Value = Composer.ShowIndex(Composer.MinIndex + i * stepValue)
          };
        }
      }
    }

    /// <summary>
    /// Enumerate values
    /// </summary>
    public virtual IEnumerable<IPointModel> GetValueEnumerator()
    {
      if (Composer?.Engine is not null)
      {
        var cnt = (double)Composer.ValueLabelCount + 1.0;
        var step = (double)Composer.Engine.ValueSize / cnt;
        var stepValue = (double)(Composer.MaxValue - Composer.MinValue) / cnt;

        for (var i = 1.0; i < cnt; i++)
        {
          var Index = step * i;
          var Value = Composer.ShowValue(Composer.MinValue + (cnt - i) * stepValue);

          yield return new PointModel
          {
            Index = step * i,
            Value = Composer.ShowValue(Composer.MinValue + (cnt - i) * stepValue)
          };
        }
      }
    }

    /// <summary>
    /// Render
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Task Update(Composer message = null)
    {
      if (message is null && Updater?.IsCompleted is false)
      {
        return Updater;
      }

      return Updater = InvokeAsync(async () =>
      {
        var engine = Composer?.Engine as CanvasEngine;

        if (engine?.Map is null)
        {
          return;
        }

        Composer.Engine.Clear();
        Composer.Update();

        using (var image = engine.Map.Encode(SKEncodedImageFormat.Webp, 100))
        {
          var data = image.ToArray();

          await Server.Stream.Writer.WriteAsync(data);
          await Server.Stream.Writer.WriteAsync(data);
        }

        if (message is not null)
        {
          OnUpdate(await CreateMessage());
        }

        StateHasChanged();
      });
    }

    /// <summary>
    /// Create canvas
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public virtual async Task Create(Action<ViewMessage> action)
    {
      Dispose(0);

      Server = await (new StreamServer()).Create();
      Service = await (new ScriptService(RuntimeService)).CreateModule();
      Service.OnSize = async o =>
      {
        action(await CreateMessage());
        await Update();
      };

      action(await CreateMessage());
      await Update();
    }

    /// <summary>
    /// Get information about event
    /// </summary>
    /// <returns></returns>
    protected async Task<ViewMessage> CreateMessage()
    {
      Bounds = await Service.GetElementBounds(CanvasContainer);

      return new ViewMessage
      {
        View = this,
        X = Bounds.X,
        Y = Bounds.Y
      };
    }

    /// <summary>
    /// Dispose
    /// </summary>
    protected void Dispose(int o)
    {
      Composer?.Dispose();
      Server?.Dispose();
    }

    /// <summary>
    /// Dispose
    /// </summary>
    void IDisposable.Dispose()
    {
      Dispose(0);
    }

    /// <summary>
    /// Get cursor position
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    protected ViewMessage GetDelta(MouseEventArgs e)
    {
      if (Composer?.Engine is null)
      {
        return null;
      }

      var values = Composer.GetValues(Composer.Engine, new PointModel
      {
        Index = e.OffsetX,
        Value = e.OffsetY
      });

      return new ViewMessage
      {
        X = e.OffsetX,
        Y = e.OffsetY,
        ValueX = Composer.ShowIndex(values.Index.Value),
        ValueY = Composer.ShowValue(values.Value)
      };
    }

    /// <summary>
    /// Mouse wheel event
    /// </summary>
    /// <param name="e"></param>
    protected void OnWheel(WheelEventArgs e)
    {
      if (Composer?.Engine is null)
      {
        return;
      }

      var isZoom = e.ShiftKey;

      switch (true)
      {
        case true when e.DeltaY > 0: _ = isZoom ? Composer.ZoomIndexScale(-1) : Composer.PanIndexScale(1); break;
        case true when e.DeltaY < 0: _ = isZoom ? Composer.ZoomIndexScale(1) : Composer.PanIndexScale(-1); break;
      }

      Update(Composer);
    }

    /// <summary>
    /// Horizontal drag and resize event
    /// </summary>
    /// <param name="e"></param>
    protected void OnMouseMove(MouseEventArgs e)
    {
      if (Composer?.Engine is null)
      {
        return;
      }

      var position = GetDelta(e);

      Cursor ??= position;

      if (e.Buttons == 1)
      {
        var isZoom = e.ShiftKey;
        var deltaX = Cursor.X - position.X;
        var deltaY = Cursor.Y - position.Y;

        switch (true)
        {
          case true when deltaX > 0: _ = isZoom ? Composer.ZoomIndexScale(-1) : Composer.PanIndexScale(1); break;
          case true when deltaX < 0: _ = isZoom ? Composer.ZoomIndexScale(1) : Composer.PanIndexScale(-1); break;
        }

        Update(Composer);
      }

      Cursor = position;
    }

    /// <summary>
    /// Resize event
    /// </summary>
    /// <param name="e"></param>
    /// <param name="direction"></param>
    protected void OnScaleMove(MouseEventArgs e, int direction = 0)
    {
      if (Composer?.Engine is null)
      {
        return;
      }

      var position = GetDelta(e);

      Move ??= position;

      if (e.Buttons == 1)
      {
        var isZoom = e.ShiftKey;
        var deltaX = Move.X - position.X;
        var deltaY = Move.Y - position.Y;

        switch (direction > 0)
        {
          case true when deltaX > 0: _ = Composer.ZoomIndexScale(-1); break;
          case true when deltaX < 0: _ = Composer.ZoomIndexScale(1); break;
        }

        switch (direction < 0)
        {
          case true when deltaY > 0: Composer.ZoomValueScale(-1); break;
          case true when deltaY < 0: Composer.ZoomValueScale(1); break;
        }

        Update(Composer);
      }

      Move = position;
    }

    /// <summary>
    /// Double clck event in the view area
    /// </summary>
    /// <param name="e"></param>
    protected void OnMouseDown(MouseEventArgs e)
    {
      if (Composer?.Engine is null)
      {
        return;
      }

      if (e.CtrlKey)
      {
        Composer.ValueDomain = null;
        Update(Composer);
      }
    }

    /// <summary>
    /// Mouse leave event
    /// </summary>
    /// <param name="e"></param>
    protected void OnMouseLeave(MouseEventArgs e)
    {
      if (Composer?.Engine is null)
      {
        return;
      }

      Cursor = null;
    }
  }
}
