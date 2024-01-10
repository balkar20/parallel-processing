using System.Collections.Frozen;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TrafficControlApp.Models.Items.Base;
using TrafficControlApp.Models.Results.Procession.Abstractions;
using TrafficControlApp.Services.Events.Abstractions;
using TrafficControlApp.Services.Events.Data.Enums;

namespace TrafficControlApp.Processors.Abstractions;

public abstract class Processor<TInput, TProcessionResult>(
        IEventLoggingService? eventLoggingService)
    : IProcessor<TInput>
where TInput: ApplicationItem<string>
where TProcessionResult: IProcessionResult
{
    #region private fields
    
    private readonly IEventLoggingService? EventLoggingService = eventLoggingService;

    private Queue<IProcessor<TInput>> ProcessorsDepended = new ();
    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    private int counter = 0;

    #endregion

    #region Public Properties

    public string ProcessorId  => Guid.NewGuid().ToString();
    
    public string InputId { get; set; }
    public string ProcessorName { get; set; }
    
    public bool IsCompletedNestedProcessing { get; set; }
    public bool IsCompletedCurrentProcessing { get; set; }
    
    public bool IsStartedSelfProcessing { get; set; }

    public IProcessor<TInput>? ProcessorFromDependentQue { get; set; }
    public IProcessor<TInput>? ParentProcessor { get; set; }

    #endregion
    
    #region Events

    public event IProcessor<TInput>.NotifyNestedProcessingCompleted NestedProcessingCompletedEvent;
    public event IProcessor<TInput>.NotifyCurrentProcessingCompleted CurrentProcessingCompletedEvent;
        
    #endregion

    #region Protected Abstract Methods

    protected abstract Task<IProcessionResult> ProcessLogic(TInput inputData);
    
    
    protected abstract Task SetProcessionResult(TProcessionResult result);

    #endregion

    #region Public Methods

    public async Task ProcessNextAsync(TInput inputData)
    {
        IsStartedSelfProcessing = true;
        ProcessorName = this.GetType().FullName;
        var threadId = Thread.CurrentThread.ManagedThreadId;
        await EventLoggingService.LogEvent(threadId.ToString(), EventLoggingTypes.ThreadIdLogging, $"|||{ProcessorName}||| with dependant: {ProcessorFromDependentQue?.ProcessorName}");
            try
            {
                if (ProcessorFromDependentQue != null)
                {
                    ProcessorFromDependentQue.CurrentProcessingCompletedEvent += async (t) => await ProcessorFromDependentQueOnCurrentProcessingCompletedEventHandler(t);
                    ProcessorFromDependentQue.NestedProcessingCompletedEvent += async () => await NestedProcessingCompletedEventHandler();
                    eventLoggingService.LogEvent("CurrentProcessingCompletedEvent",
                        EventLoggingTypes.SubscribingToEvent, ProcessorName);
                }
                
                EventLoggingService.LogEvent($"Entering {ProcessorName}", EventLoggingTypes.ProcessionInformation);
                await semaphoreSlim.WaitAsync();
                EventLoggingService.LogEvent($"Entered {ProcessorName}", EventLoggingTypes.ProcessionInformation);
                try
                {
                    await DoProcessionOnCurrentProcessingCompletedAsync(inputData);
                    
                }
                finally
                {
                    EventLoggingService.LogEvent($"Releasing {ProcessorName}", EventLoggingTypes.ProcessionInformation);
                    semaphoreSlim.Release();
                    EventLoggingService.LogEvent($"Released {ProcessorName}", EventLoggingTypes.ProcessionInformation);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
    }

    #region Event Handlers
    
    private async Task ProcessorFromDependentQueOnCurrentProcessingCompletedEventHandler(IProcessor<TInput> processor)
    {
        await EventLoggingService.LogEvent($"ProcessorFromDependentQueOnCurrentProcessingCompletedEventHandler on {this.ProcessorName}", EventLoggingTypes.HandlingEvent, processor.ProcessorName);
    }

    private async Task NestedProcessingCompletedEventHandler()
    {
        await EventLoggingService.LogEvent("NestedProcessingCompletedEventHandler", EventLoggingTypes.HandlingEvent);
    }

    #endregion

    private async Task DoProcessionOnCurrentProcessingCompletedAsync(TInput inputData)
    {
        if (ProcessorFromDependentQue != null && ProcessorFromDependentQue.IsCompletedCurrentProcessing && ProcessorFromDependentQue.IsStartedSelfProcessing)
        {
            await EventLoggingService.LogEvent(ProcessorName,
                EventLoggingTypes.CallMethodInProcessorWithCompletedDependant, ProcessorFromDependentQue.ProcessorName);
            if (!ProcessorFromDependentQue.IsCompletedNestedProcessing)
            {
                await ProcessorFromDependentQue.ProcessNextAsync(inputData);
            }
            return;
        }
        
        if (ProcessorFromDependentQue != null && !ProcessorFromDependentQue.IsCompletedCurrentProcessing && !ProcessorFromDependentQue.IsStartedSelfProcessing)
        {
            await ProcessorFromDependentQue.ProcessNextAsync(inputData);
                
            SetNextProcessorFromDependent(ProcessorFromDependentQue);
            return;
        }
        
        var processionResult = await ProcessLogic(inputData);
        IsCompletedCurrentProcessing = true;
        if (CurrentProcessingCompletedEvent != null)
        {
            await CurrentProcessingCompletedEvent(this);
        }
            
        SetNextProcessor();
        await EventLoggingService.LogEvent($"Leave DPA: {this.ProcessorId} | ItemId: {this.InputId}, {this.ProcessorId}", EventLoggingTypes.ProcessionInformation);
    }

    private async void SetNextProcessorFromDependent(IProcessor<TInput> processorFromDependentQue)
    {
        if (processorFromDependentQue.IsCompletedNestedProcessing)
        {
            ProcessorFromDependentQue = GetNextProcessorFromDependants();
            if (IsCompletedNestedProcessing)
            {
                if (NestedProcessingCompletedEvent != null)
                {
                    await NestedProcessingCompletedEvent?.Invoke();
                }
                
                return;
            }

            if (IsCompletedCurrentProcessing)
            {
                if (CurrentProcessingCompletedEvent != null)
                {
                     await CurrentProcessingCompletedEvent.Invoke(this);
                }

            }
        }
    }

    public void AddDependentProcessor(IProcessor<TInput> dependentProcessor)
    {
        ProcessorsDepended.Enqueue(dependentProcessor);
        dependentProcessor.ParentProcessor = this;
    }

    protected async Task SetTrue(TInput input)
    {
        IsCompletedCurrentProcessing = true;
    }

    #endregion


    #region Private Methods

    protected IProcessor<TInput>? GetNextProcessorFromDependants()
    {
        ProcessorsDepended.TryDequeue(out IProcessor<TInput>? proc);
        if (proc == null)
        {
            IsCompletedNestedProcessing = true;
        }

        return proc;
    }
    
    protected void SetNextProcessor()
    {
        ProcessorFromDependentQue = GetNextProcessorFromDependants();
        //todo rise event allowing ProcessNext in Parallel
    }

    public void SetDependents(Queue<IProcessor<TInput>> dependents)
    {
        ProcessorsDepended = dependents;
    }

    #endregion
}