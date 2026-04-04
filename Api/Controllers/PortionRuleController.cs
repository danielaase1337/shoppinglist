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
    public class PortionRuleController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<PortionRule> _repository;
        private readonly IMapper _mapper;

        public PortionRuleController(ILoggerFactory loggerFactory, IGenericRepository<PortionRule> repository, IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<PortionRuleController>();
            _repository = repository;
            _mapper = mapper;
        }

        [Function("portionrules")]
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
                        _logger.LogError("Could not get any portion rules");
                        return await GetErroRespons("Could not get any portion rules", req);
                    }
                    else if (!result.Any())
                    {
                        _logger.LogInformation("No portion rules found");
                        await response.WriteAsJsonAsync(new List<PortionRuleModel>());
                        return response;
                    }

                    var activeRules = result
                        .Where(r => r.IsActive)
                        .OrderBy(r => r.ShopItemId)
                        .ThenBy(r => r.AgeGroup)
                        .ToArray();
                    var models = _mapper.Map<PortionRuleModel[]>(activeRules);
                    await response.WriteAsJsonAsync(models);
                    return response;
                }
                else if (req.Method == "POST")
                {
                    var requestBody = await req.ReadFromJsonAsync<PortionRuleModel>();
                    if (requestBody == null) return await GetNoContentRespons("No portion rule to create", req);

                    var rule = _mapper.Map<PortionRule>(requestBody);
                    rule.LastModified = DateTime.UtcNow;
                    rule.IsActive = true;

                    var addRes = await _repository.Insert(rule);
                    if (addRes == null)
                    {
                        _logger.LogWarning("Could not create portion rule");
                        return await GetErroRespons("Could not create portion rule", req);
                    }

                    await response.WriteAsJsonAsync(_mapper.Map<PortionRuleModel>(addRes));
                    return response;
                }
                else if (req.Method == "PUT")
                {
                    var requestBody = await req.ReadFromJsonAsync<PortionRuleModel>();
                    if (requestBody == null) return await GetNoContentRespons("No portion rule to update", req);

                    var rule = _mapper.Map<PortionRule>(requestBody);
                    rule.LastModified = DateTime.UtcNow;

                    var updateRes = await _repository.Update(rule);
                    if (updateRes == null)
                        return await GetErroRespons("Could not update portion rule", req);

                    await response.WriteAsJsonAsync(_mapper.Map<PortionRuleModel>(updateRes));
                    return response;
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong in execution of portionrules, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        [Function("portionrule")]
        public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Anonymous, "get", "delete", Route = "portionrule/{id}")] HttpRequestData req, string id)
        {
            try
            {
                if (req.Method == "GET")
                {
                    var result = await _repository.Get(id);
                    if (result == null)
                    {
                        _logger.LogInformation($"Could not find portion rule with id {id}");
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }

                    var model = _mapper.Map<PortionRuleModel>(result);
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
                        _logger.LogWarning($"Could not find portion rule with id {id} for soft delete");
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }

                    item.IsActive = false;
                    item.LastModified = DateTime.UtcNow;

                    var updatedItem = await _repository.Update(item);
                    if (updatedItem == null)
                        return await GetErroRespons($"Could not soft-delete portion rule with id {id}", req);

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(_mapper.Map<PortionRuleModel>(updatedItem));
                    return response;
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong in execution of portionrule/{id}, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }
    }
}
