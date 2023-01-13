namespace SpaPrerenderer.Configs;

public class KestrelCustomConfig
{
    public List<KestrelCustomEndpoint>? Endpoints { get; set; }
}

public class KestrelCustomEndpoint
{
    public bool IsUnixSocket { get; set; }
    public string? Path { get; set; }
    public KestrelCustomHttpsConfig? Https { get; set; }
}

public class KestrelCustomHttpsConfig
{
    public string? CertPath { get; set; }
    public string? CertPassword { get; set; }
}