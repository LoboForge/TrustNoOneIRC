using System;
using System.IO;
using System.Text.Json;

public class PlayerService
{
	private readonly string _basePath = Path.Combine("Data", "Players");

	public PlayerService()
	{
		Directory.CreateDirectory(_basePath);
	}

	public PlayerProfile? Load(string nick)
	{
		var file = GetPath(nick);
		if (!File.Exists(file)) return null;

		var json = File.ReadAllText(file);
		return JsonSerializer.Deserialize<PlayerProfile>(json);
	}

	public void Save(PlayerProfile profile)
	{
		var file = GetPath(profile.Nick);
		var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(file, json);
	}

	private string GetPath(string nick) =>
		Path.Combine(_basePath, $"{nick.ToLowerInvariant()}.json");
}
