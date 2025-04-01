
using Microsoft.AspNetCore.Mvc;
using API_POKEMON.Dtos;
using System.Text.Json;

namespace API_POKEMON.Controllers;

[ApiController]
[Route("[controller]")]
public class PokemonMelhoradoController : ControllerBase
{
    [HttpGet("{quantityOfPokemons:int}")]
    public async Task<ActionResult> GetPokemonsGroupByColorAsync(int quantityOfPokemons)
    {
        if (quantityOfPokemons <= 0 || quantityOfPokemons > 100)
            return BadRequest("Quantidade de Pokemons deve ser entre 1 e 100.");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var client = new HttpClient();

            var response = await client.GetAsync($"https://pokeapi.co/api/v2/pokemon?limit={quantityOfPokemons}&offset=0");
            if (!response.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Erro ao consultar API de Pokemons: {response.ReasonPhrase}");

            var content = await response.Content.ReadAsStringAsync();
            var deserializedContent = JsonSerializer.Deserialize<ResponseGetPokemonsDto>(content)
                ?? throw new Exception("Erro ao ler resposta de pokeapi.");

            var results = deserializedContent.results;
            if (results == null || !results.Any())
                return StatusCode(StatusCodes.Status500InternalServerError, "Objeto que contém os Pokemons inválido.");

            // Processa as requisições para cada Pokémon de forma paralela.
            var tasks = results.Select<PokemonDto, Task<(string? Name, string? Color, string? Error)>>(
                async (PokemonDto pokemon) =>
                {
                    var pokemonName = pokemon.name;
                    if (string.IsNullOrWhiteSpace(pokemonName))
                        return (Name: null, Color: null, Error: "Pokemon com nome vazio.");

                    try
                    {
                        var responseSpecie = await client.GetAsync($"https://pokeapi.co/api/v2/pokemon-species/{pokemonName}");
                        if (!responseSpecie.IsSuccessStatusCode)
                            return (Name: pokemonName, Color: null, Error: $"Erro ao consultar espécie do Pokemon {pokemonName}: {responseSpecie.ReasonPhrase}");

                        string contentSpecie = await responseSpecie.Content.ReadAsStringAsync();
                        var deserializedContentSpecie = JsonSerializer.Deserialize<ResponseGetPokemonSpeciesDto>(contentSpecie)
                            ?? throw new Exception("Erro ao ler resposta de pokeapi/species.");

                        var color = deserializedContentSpecie.color;
                        if (color == null || string.IsNullOrWhiteSpace(color.name))
                            return (Name: pokemonName, Color: null, Error: $"Não foi possível identificar a cor do Pokemon: {pokemonName}");

                        return (Name: pokemonName, Color: color.name, Error: null);
                    }
                    catch (Exception ex)
                    {
                        return (Name: pokemonName, Color: null, Error: $"Erro ao processar o Pokemon {pokemonName}: {ex.Message}");
                    }
                });

            var pokemonResults = await Task.WhenAll(tasks);

            // Agrupa os resultados usando um dicionário para melhor performance.
            var groupedDict = new Dictionary<string, List<string>>();
            var errors = new List<string>();

            foreach (var (Name, Color, Error) in pokemonResults)
            {
                if (!string.IsNullOrWhiteSpace(Error))
                {
                    errors.Add(Error);
                }
                else
                {
                    if (!groupedDict.ContainsKey(Color!))
                        groupedDict[Color!] = new List<string> { Name! };
                    else
                        groupedDict[Color!].Add(Name!);
                }
            }

            // Converte o dicionário para a lista de DTOs.
            var listPokemonGroupedByColor = groupedDict.Select(g => new PokemonGroupedByColorDto
            {
                Color = g.Key,
                Names = g.Value
            }).ToList();

            // var listPokemonGroupedByColor = pokemonResults
            //     .Where(p => string.IsNullOrWhiteSpace(p.Error))
            //     .GroupBy(p => p.Color)
            //     .Select(g => new PokemonGroupedByColorDto
            //     {
            //         Color = g.Key,
            //         Names = g.Select(p => p.Name).ToList()
            //     })
            //     .ToList();

            stopwatch.Stop();
            var responseTime = stopwatch.ElapsedMilliseconds;

            return Ok(new { listPokemonGroupedByColor, errors, responseTime });
        }
        catch (ArgumentException ex)
        {
            stopwatch.Stop();
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Ocorreu um erro interno ao consultar os Pokemons: {ex.Message}");
        }
    }
}
