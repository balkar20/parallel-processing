using AutoMapper;
using TrafficControlApp.Models;
using TrafficControlApp.Models.Items.Analysing;
using TrafficControlApp.Models.Results;
using TrafficControlApp.Models.Results.Procession.Abstractions;
using TrafficControlApp.Processors.Abstractions;
using TrafficControlApp.Services;
using TrafficControlApp.Services.Analysers.Abstractions;
using TrafficControlApp.Services.Events.Abstractions;

namespace TrafficControlApp.Processors;

public class VehicleDangerProcessor(IProcessingItemsStorageServiceRepository<string, Track> processingItemsStorageServiceRepository,
    IAnalyzerService analyzerService,
    IMapper mapper,
    IEventLoggingService eventLoggingService)
    : Processor<Track>(eventLoggingService)
{
    protected override async Task<IProcessionResult> ProcessLogic(Track inputData)
    {
        // var vehicles = await _sharedMemoryService.ProcessingItemsStorageService.G(inputData.ItemId);
        
        var analysingItem = mapper.Map<TypeAnalysingItem>(inputData);
        var typeAnaliseResult = await analyzerService.Analyse(analysingItem);
        var typeProcessionResult = mapper.Map<VehicleDangerProcessionResult>(typeAnaliseResult);
        return typeProcessionResult;
    }

    private async Task WorkWithDependentData(string processingItemId)
    {
         // VehicleTypeProcessionResult dependentData = _sharedMemoryService.ProcessingItemsStorageServiceRepository.GetProcessingItem(processingItemId);
         // Console.WriteLine($"DependentDta(VehicleColorStatistics) Message: {dependentData.Message}");
    }
}