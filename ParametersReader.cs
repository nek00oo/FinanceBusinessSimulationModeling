using Newtonsoft.Json;

namespace SimulationModeling;

public static class ParametersReader
{
    public static Parameters? ReadParametersFromJson(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var parameters = JsonConvert.DeserializeObject<Parameters>(json);
            if (parameters == null)
            {
                Console.WriteLine("Ошибка десериализации параметров из JSON.");
                return null;
            }

            return parameters;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка чтения параметров из JSON-файла: {ex.Message}");
            return null;
        }
    }
}

public record Parameters(
    int Iterations,
    int Employees,
    double Salary,
    int AverageClientsMonth,
    double MeanCostOrder,
    double OrderStdDev,
    int Alpha,
    int Beta);