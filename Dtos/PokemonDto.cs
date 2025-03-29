namespace API_POKEMON.Dtos;

public class PokemonDto
{
    public string? name { get; set; }
}

public class ResponseGetPokemonsDto
{
    public List<PokemonDto>? results { get; set; }
}

public class PokemonGroupedByColorDto
{
    public string Color { get; set; } = string.Empty;
    public List<string> Names { get; set; } = new List<string>();
}

public class PokemonColorDto
{
    public string? name { get; set; }
}

public class ResponseGetPokemonSpeciesDto
{
    public PokemonColorDto? color { get; set; }
}