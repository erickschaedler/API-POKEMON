using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Text.Json;
using API_POKEMON.Dtos;
using System.Data;

namespace API_POKEMON.Controllers;

[ApiController]
[Route("[controller]")]
public class PokemonController1 : ControllerBase
{
    private readonly string _connectionString = "User Id=seuUsuario;Password=suaSenha;Data Source=seuDataSource";

    [HttpGet]
    public IActionResult ListarPokemons()
    {
        var pokemons = new List<PokemonCorDto>();

        using (OracleConnection conn = new OracleConnection(_connectionString))
        {
            conn.Open();
            string query = @"
                    SELECT p.NOME_POKEMON, c.NOME_COR
                    FROM POKEMONS p
                    JOIN CORES c ON p.ID_COR = c.ID_COR
                ";

            using OracleCommand cmd = new OracleCommand(query, conn);
            using OracleDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                pokemons.Add(new PokemonCorDto
                {
                    Nome = reader["NOME_POKEMON"].ToString() ?? "",
                    Cor = reader["NOME_COR"].ToString() ?? ""
                });
            }
        }

        return Ok(pokemons);
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

            foreach (var pokemonGroup in listPokemonsGroupedByColor)
            {
                var idCor = await InserirCorAsync(pokemonGroup.Color);

                foreach (var pokemon in pokemonGroup.Names)
                {
                    await InserirPokemonNoBancoAsync(pokemon, idCor);
                }
            }

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

    // private int InserirCor(string nomeCor)
    // {
    //     int idCor = 0;
    //     using (OracleConnection conn = new(_connectionString))
    //     {
    //         conn.Open();
    //         string selectQuery = "SELECT ID_COR FROM CORES WHERE NOME_COR = :nome";
    //         string insertQuery = "INSERT INTO CORES (NOME_COR) VALUES (:nome)";

    //         using (OracleCommand cmd = new(insertQuery, conn))
    //         {
    //             cmd.Parameters.Add(new OracleParameter("nome", nomeCor));
    //             cmd.ExecuteNonQuery();
    //         }
    //         conn.Commit();

    //         using (OracleCommand cmd = new(selectQuery, conn))
    //         {
    //             cmd.Parameters.Add(new OracleParameter("nome", nomeCor));
    //             object result = cmd.ExecuteScalar();
    //             if (result != null)
    //             {
    //                 idCor = Convert.ToInt32(result);
    //             }
    //         }
    //     }

    //     return idCor;
    // }

    private async Task<int> InserirCorAsync(string nomeCor)
    {
        using OracleConnection conn = new(_connectionString);
        await conn.OpenAsync();

        string query = "INSERT INTO CORES (NOME_COR) VALUES (:nome) RETURNING ID_COR INTO :id";
        using OracleCommand cmd = new(query, conn);
        cmd.Parameters.Add(new OracleParameter("nome", nomeCor));
        OracleParameter idParam = new("id", OracleDbType.Int32, ParameterDirection.Output);
        cmd.Parameters.Add(idParam);

        await cmd.ExecuteNonQueryAsync();
        conn.Commit();

        return Convert.ToInt32(idParam.Value);
    }

    private async Task InserirPokemonNoBancoAsync(string nomePokemon, int idCor)
    {
        using OracleConnection conn = new(_connectionString);
        await conn.OpenAsync();

        string insertQuery = "INSERT INTO POKEMONS (NOME_POKEMON, ID_COR) VALUES (:nome, :id_cor)";
        using OracleCommand cmd = new(insertQuery, conn);
        cmd.Parameters.Add(new OracleParameter("nome", nomePokemon));
        cmd.Parameters.Add(new OracleParameter("id_cor", idCor));

        await cmd.ExecuteNonQueryAsync();
        conn.Commit();
    }
}