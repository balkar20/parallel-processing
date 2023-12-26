using System.Threading.Tasks.Dataflow;
using AutoMapper;
using DataFlowProducerConsumer.Models;
using DataFlowProducerConsumer.Models.Results;
using DataFlowProducerConsumer.Processors.Abstractions;
using DataFlowProducerConsumer.Services;
using DataFlowProducerConsumer.Services.Storage;

namespace DataFlowProducerConsumer.Processors;

public class VehicleTypeProcessor: Processor<Track>
{
    private readonly ISharedMemoryVehicleService _sharedMemoryService;
    private readonly IVehicleAnalyzerService<VehicleTypeProcessResult> _analyserService;
    private readonly IMapper _mapper;

    public VehicleTypeProcessor(ISharedMemoryVehicleService sharedMemoryService)
    {
        _sharedMemoryService = sharedMemoryService;
    }

    public override async Task<IProcessResult> ProcessLogic(Track inputData)
    {
        var vehicles = await _sharedMemoryService.GetVehicleDataByTrackId(inputData.TrackId);
        
        foreach (var vehicle in vehicles)
        {
            var vehicleType = vehicle.VehicleType;
            
        }
        
        int bytesProcessed = 0;
        
        var testVeh = new Vehicle();
        var typeAnaliseResult = await _analyserService.Analyse(testVeh);
        VehicleTypeProcessResult result = _mapper.Map<VehicleTypeProcessResult>(typeAnaliseResult);
        _sharedMemoryService.VehicleTypeProcessResultDictionary.Add(inputData.TrackId, result);
        return result;
    }
}