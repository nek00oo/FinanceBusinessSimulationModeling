using Newtonsoft.Json;

namespace SimulationModeling;

public static class ParametersReader
{
    public static Parameters? ReadParametersFromJson(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var parameters = JsonConvert.DeserializeObject<Parameters>(json);
        if (parameters == null)
            throw new JsonReaderException("Ошибка десериализации параметров из JSON.");

        return parameters;
    }
}