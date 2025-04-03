namespace API_POKEMON.Dtos;

public class ResultDto
{
    public string? name { get; set; }
}

public class ResponseGetPokemonsDto
{
    public List<ResultDto>? results { get; set; }
}

public class ColorDto
{
    public string? name { get; set; }
}

public class ResultGetSpecieDto
{
    public ColorDto? color { get; set; }
}

public class PokemonsGroupedByColorDto
{
    public string Color { get; set; } = string.Empty;
    public List<string> Names { get; set; } = new List<string>();
}

public class PokemonCorDto
{
    public string Nome { get; set; } = string.Empty;
    public string Cor { get; set; } = string.Empty;
}