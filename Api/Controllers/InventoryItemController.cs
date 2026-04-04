using AutoMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Shared.FireStoreDataModels;
using Shared.HandlelisteModels;
using Shared.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Api.Controllers
{
    public class InventoryAdjustmentModel
    {
        public string Id { get; set; }
        public double QuantityDelta { get; set; }  // positive = add, negative = deduct
    }

    public class InventoryItemController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<InventoryItem> _repository;
        private readonly IMapper _mapper;

        public InventoryItemController(ILoggerFactory loggerFactory, IGenericRepository<InventoryItem> repository, IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<InventoryItemController>();
            _repository = repository;
            _mapper = mapper;
        }

        [Function("inventoryitems")]
        public async Task<HttpResponseData> RunAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put")] HttpRequestData req)
        {
            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);

                if (req.Method == "GET")
                {
                    var result = await _repository.Get();

                    if (result == null)
                    {
                        _logger.LogError("Could not get any inventory items");
                        return await GetErroRespons("Could not get any inventory items", req);
                    }
                    else if (!result.Any())
                    {
                        _logger.LogInformation("No inventory items found");
                        await response.WriteAsJsonAsync(new List<InventoryItemModel>());
                        return response;
                    }

                    var activeItems = result.Where(i => i.IsActive).OrderBy(i => i.Name).ToArray();
                    var models = _mapper.Map<InventoryItemModel[]>(activeItems);
                    await response.WriteAsJsonAsync(models);
                    return response;
                }
                else if (req.Method == "POST")
                {
                    var requestBody = await req.ReadFromJsonAsync<InventoryItemModel>();
                    if (requestBody == null) return await GetNoContentRespons("No inventory item to create", req);

                    var item = _mapper.Map<InventoryItem>(requestBody);
                    item.LastModified = DateTime.UtcNow;
                    item.IsActive = true;

                    var addRes = await _repository.Insert(item);
                    if (addRes == null)
                    {
                        _logger.LogWarning("Could not create inventory item");
                        return await GetErroRespons("Could not create inventory item", req);
                    }

                    await response.WriteAsJsonAsync(_mapper.Map<InventoryItemModel>(addRes));
                    return response;
                }
                else if (req.Method == "PUT")
                {
                    var requestBody = await req.ReadFromJsonAsync<InventoryItemModel>();
                    if (requestBody == null) return await GetNoContentRespons("No inventory item to update", req);

                    var item = _mapper.Map<InventoryItem>(requestBody);
                    item.LastModified = DateTime.UtcNow;

                    var updateRes = await _repository.Update(item);
                    if (updateRes == null)
                        return await GetErroRespons("Could not update inventory item", req);

                    await response.WriteAsJsonAsync(_mapper.Map<InventoryItemModel>(updateRes));
                    return response;
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong in execution of inventoryitems, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        [Function("inventoryitem")]
        public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Anonymous, "get", "delete", Route = "inventoryitem/{id}")] HttpRequestData req, string id)
        {
            try
            {
                if (req.Method == "GET")
                {
                    var result = await _repository.Get(id);
                    if (result == null)
                    {
                        _logger.LogInformation($"Could not find inventory item with id {id}");
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }

                    var model = _mapper.Map<InventoryItemModel>(result);
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(model);
                    return response;
                }
                else if (req.Method == "DELETE")
                {
                    // Soft delete: mark inactive rather than remove from Firestore
                    var item = await _repository.Get(id);
                    if (item == null)
                    {
                        _logger.LogWarning($"Could not find inventory item with id {id} for soft delete");
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }

                    item.IsActive = false;
                    item.LastModified = DateTime.UtcNow;

                    var updatedItem = await _repository.Update(item);
                    if (updatedItem == null)
                        return await GetErroRespons($"Could not soft-delete inventory item with id {id}", req);

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(_mapper.Map<InventoryItemModel>(updatedItem));
                    return response;
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong in execution of inventoryitem/{id}, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        [Function("inventoryitemsadjust")]
        public async Task<HttpResponseData> RunAdjust([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "inventoryitems/adjust")] HttpRequestData req)
        {
            try
            {
                var adjustments = await req.ReadFromJsonAsync<List<InventoryAdjustmentModel>>();
                if (adjustments == null || !adjustments.Any())
                    return await GetNoContentRespons("No adjustments to apply", req);

                // Fetch all inventory once to avoid N+1 per adjustment
                var allInventory = await _repository.Get();

                var updated = new List<InventoryItemModel>();

                foreach (var adjustment in adjustments)
                {
                    var item = allInventory?.FirstOrDefault(i => i.Id == adjustment.Id);
                    if (item == null)
                    {
                        _logger.LogWarning($"Could not find inventory item with id {adjustment.Id} for adjustment");
                        continue;
                    }

                    item.QuantityInStock += adjustment.QuantityDelta;
                    if (item.QuantityInStock < 0) item.QuantityInStock = 0;
                    item.LastModified = DateTime.UtcNow;

                    var updateRes = await _repository.Update(item);
                    if (updateRes != null)
                        updated.Add(_mapper.Map<InventoryItemModel>(updateRes));
                    else
                        _logger.LogWarning($"Could not update inventory item {adjustment.Id} after adjustment");
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(updated);
                return response;
            }
            catch (Exception e)
            {
                var msg = "Something went wrong during inventory adjustment";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }
    }
}
