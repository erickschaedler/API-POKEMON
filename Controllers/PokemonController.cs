
using Microsoft.AspNetCore.Mvc;
using API_POKEMON.Dtos;
using System.Text.Json;

namespace API_POKEMON.Controllers;

[ApiController]
[Route("[controller]")]
public class PokemonController : ControllerBase
{
    [HttpGet("{quantityOfPokemons:int}")]
    public async Task<ActionResult> GetPokemonsGroupByColorAsync(int quantityOfPokemons)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            if (quantityOfPokemons == 0 || quantityOfPokemons > 100)
                throw new ArgumentException("Quantidade de Pokemons deve ser entre 1 e 100.");

            using var client = new HttpClient();

            var response = await client.GetAsync($"https://pokeapi.co/api/v2/pokemon?limit={quantityOfPokemons}&offset=0");
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Erro ao consultar API de Pokemons: {response.ReasonPhrase}.");

            string content = await response.Content.ReadAsStringAsync();
            var deserializedContent = JsonSerializer.Deserialize<ResponseGetPokemonsDto>(content)
                ?? throw new Exception("Erro ao ler resposta de pokeapi.");

            var results = deserializedContent.results;

            if (results == null || !results.Any())
                throw new Exception("Objeto que contém os Pokemons inválido.");

            var errors = new List<string>();
            var listPokemonGroupedByColor = new List<PokemonGroupedByColorDto>();

            foreach (var pokemon in results)
            {
                var pokemonName = pokemon.name;

                if (string.IsNullOrWhiteSpace(pokemonName))
                {
                    errors.Add("Pokemon com nome vazio.");
                    continue;
                }

                var responseSpecie = await client.GetAsync($"https://pokeapi.co/api/v2/pokemon-species/{pokemonName}");
                if (!responseSpecie.IsSuccessStatusCode)
                {
                    errors.Add($"Erro ao consultar espécie do Pokemon {pokemonName}: {responseSpecie.ReasonPhrase}");
                    continue;
                }

                string contentSpecie = await responseSpecie.Content.ReadAsStringAsync();
                var deserializedContentSpecie = JsonSerializer.Deserialize<ResponseGetPokemonSpeciesDto>(contentSpecie)
                    ?? throw new Exception("Erro ao ler resposta de pokeapi/species.");

                var color = deserializedContentSpecie.color;

                if (color == null)
                {
                    errors.Add($"Não foi possível identificar a cor do Pokemon: {pokemonName}");
                    continue;
                }

                var colorName = color.name;

                if (string.IsNullOrWhiteSpace(colorName))
                {
                    errors.Add($"Nome da cor do Pokemon {pokemonName} está vazia");
                    continue;
                }

                var findListPokemonsByColor = listPokemonGroupedByColor.FirstOrDefault(x => x.Color == colorName);

                if (findListPokemonsByColor == null)
                {
                    var newPokemonGroupedByColorDto = new PokemonGroupedByColorDto
                    {
                        Color = colorName,
                        Names = new List<string>() { pokemonName }
                    };

                    listPokemonGroupedByColor.Add(newPokemonGroupedByColorDto);
                }
                else
                {
                    findListPokemonsByColor.Names.Add(pokemonName);
                }
            }

            stopwatch.Stop();
            var responseTime = stopwatch.ElapsedMilliseconds;

            return Ok(new { listPokemonGroupedByColor, errors, responseTime });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return ex switch
            {
                ArgumentException => BadRequest(ex.Message),
                _ => StatusCode(StatusCodes.Status500InternalServerError, $"Ocorreu um erro interno ao consultar os Pokemons: {ex.Message}")
            };
        }
    }
}
