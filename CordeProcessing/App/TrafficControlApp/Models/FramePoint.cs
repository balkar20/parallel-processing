using System.Drawing;

namespace TrafficControlApp.Models;

public struct FramePoint
{
    public Point Point { get; set; }
    
    public DateTime Time { get; set; }
}