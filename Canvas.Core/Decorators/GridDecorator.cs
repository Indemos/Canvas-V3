using Canvas.Core.EngineSpace;
using Canvas.Core.ModelSpace;

namespace Canvas.Core.DecoratorSpace
{
  public class GridDecorator : BaseDecorator, IDecorator
  {
    /// <summary>
    /// Create index
    /// </summary>
    /// <param name="engine"></param>
    public virtual void CreateIndex(IEngine engine)
    {
      var shape = Composer.Line;
      var count = Composer.ValueCount;
      var step = engine.Y / count;
      var points = new IItemModel[2]
      {
        new ItemModel(),
        new ItemModel()
      };

      for (var i = 0; i < count; i++)
      {
        points[0].X = 0;
        points[0].Y = step * i;
        points[1].X = engine.X;
        points[1].Y = step * i;

        engine.CreateLine(points, shape);
      }

      points[0].X = 0;
      points[0].Y = engine.Y - 1;
      points[1].X = engine.X;
      points[1].Y = engine.Y - 1;

      engine.CreateLine(points, shape);
    }

    /// <summary>
    /// Create value
    /// </summary>
    /// <param name="engine"></param>
    public virtual void CreateValue(IEngine engine)
    {
      var shape = Composer.Line;
      var count = Composer.IndexCount;
      var step = engine.X / count;
      var points = new IItemModel[2]
      {
        new ItemModel(),
        new ItemModel()
      };

      for (var i = 0; i < count; i++)
      {
        points[0].X = step * i;
        points[0].Y = 0;
        points[1].X = step * i;
        points[1].Y = engine.Y;

        engine.CreateLine(points, shape);
      }

      points[0].X = engine.X - 1;
      points[0].Y = 0;
      points[1].X = engine.X - 1;
      points[1].Y = engine.Y;

      engine.CreateLine(points, shape);
    }
  }
}