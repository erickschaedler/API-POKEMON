using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace API_POKEMON.Controllers;

[ApiController]
[Route("[controller]")]
public class PokemonController1 : ControllerBase
{
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

    [HttpGet]
    [Route("pokemonsGroupedByColor/{quantity:int}")]
    public async Task<ActionResult> GetPokemonsGroupedByColorAsync(int quantity)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            if (quantity < 1 || quantity > 100)
                throw new ArgumentException("Informe uma quantidade entre 1 e 100");

            using var client = new HttpClient();
            var baseApi = "https://pokeapi.co/api/v2/";

            var response = await client.GetAsync($"{baseApi}pokemon?limit={quantity}&offset=0");
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Erro ao consultar API de pokemons: {baseApi}.");

            var responseContent = await response.Content.ReadAsStringAsync();
            var deserializedContent = JsonSerializer.Deserialize<ResponseGetPokemonsDto>(responseContent)
                ?? throw new Exception("Erro ao deserializar resposta da API de pokemons.");

            var results = deserializedContent.results;

            if (results == null || !results.Any())
                throw new Exception("Objeto `results` que contém os pokemosn está inválido.");

            var tasks = results.Select<ResultDto, Task<(string? Name, string? Color, string? Error)>>(
                async pokemon =>
                {
                    var pokemonName = pokemon.name;
                    if (string.IsNullOrWhiteSpace(pokemonName))
                        return (Name: null, Color: null, Error: "Pokemon com nome vazio.");

                    try
                    {
                        var responseSpecie = await client.GetAsync($"{baseApi}pokemon-species/{pokemonName}");
                        if (!responseSpecie.IsSuccessStatusCode)
                            return (Name: pokemonName, Color: null, Error: $"Erro ao cunsultar espécie do pokemon: {pokemonName}.");

                        var responseContent = await responseSpecie.Content.ReadAsStringAsync();
                        var deserializedSpecie = JsonSerializer.Deserialize<ResultGetSpecieDto>(responseContent);

                        if (deserializedSpecie == null || deserializedSpecie.color == null)
                            return (Name: pokemonName, Color: null, Error: $"Erro ao deserializar espécie do pokemon:{pokemonName}.");

                        var colorName = deserializedSpecie.color.name;

                        if (string.IsNullOrWhiteSpace(colorName))
                            return (Name: pokemonName, Color: null, Error: "Cor com nome vazia.");

                        return (Name: pokemonName, Color: colorName, Error: null);
                    }
                    catch (Exception ex)
                    {
                        return (Name: pokemonName, Color: null, Error: $"Erro ao buscar cor do pokemon {pokemonName}: {ex.Message}.");
                    }
                });

            var pokemonResults = await Task.WhenAll(tasks);
            var errors = new List<string>();
            var listPokemonsGroupedByColor = new List<PokemonsGroupedByColorDto>();

            foreach (var (Nome, Color, Error) in pokemonResults)
            {
                if (!string.IsNullOrWhiteSpace(Error))
                {
                    errors.Add(Error);
                }
                else
                {
                    var pokemonColor = listPokemonsGroupedByColor.FirstOrDefault(x => x.Color == Color);

                    if (pokemonColor != null)
                    {
                        pokemonColor.Names.Add(Nome!);
                    }
                    else
                    {
                        var newAokemonsColor = new PokemonsGroupedByColorDto
                        {
                            Color = Color!,
                            Names = new List<string>() { Nome! },
                        };

                        listPokemonsGroupedByColor.Add(newAokemonsColor);
                    }
                }
            }

            // ou

            // var groupedDict = new Dictionary<string, List<string>>();

            // foreach (var (Name, Color, Error) in pokemonsResults)
            // {
            //     if (!string.IsNullOrWhiteSpace(Error))
            //     {
            //         errors.Add(Error);
            //     }
            //     else
            //     {
            //         if (!groupedDict.ContainsKey(Color!))
            //             groupedDict[Color!] = new List<string> { Name! };
            //         else
            //             groupedDict[Color!].Add(Name!);
            //     }
            // }

            // var listPokemonGroupedByColor = groupedDict.Select(g => new PokemonsGroupedByColorDto
            // {
            //     Color = g.Key,
            //     Names = g.Value
            // }).ToList();

            stopwatch.Stop();
            var responseTime = stopwatch.ElapsedMilliseconds;

            return Ok(new { errors, listPokemonsGroupedByColor, responseTime });
        }
        catch (ArgumentException aex)
        {
            stopwatch.Stop();
            return BadRequest(aex.Message);
        }
        catch (HttpRequestException hre)
        {
            stopwatch.Stop();
            return BadRequest(hre.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return StatusCode(StatusCodes.Status500InternalServerError, $"Erro interno ao consultar os pokemons: {ex.Message}");
        }
    }
}