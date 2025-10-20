namespace BotBash.Core;

static class Extensions
{
  //should allow adding tuples to eachother
  public static (int x, int y) Add(this (int x, int y) tuple1, (int Xmove, int Ymove) tuple2)
    => (tuple1.x + tuple2.Xmove, tuple1.y + tuple2.Ymove);
}