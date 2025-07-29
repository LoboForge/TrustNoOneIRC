using System;

public class PlayerProfile
{
    public string Nick { get; set; } = "";
    public string Handle { get; set; } = "";
    public int Level { get; set; } = 1;
    public int XP { get; set; } = 0;
    public string CurrentNode { get; set; } = "root";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsEnrolled = false;
}