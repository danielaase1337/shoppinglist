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
    public class FamilyProfileController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IGenericRepository<FamilyProfile> _repository;
        private readonly IMapper _mapper;

        public FamilyProfileController(ILoggerFactory loggerFactory, IGenericRepository<FamilyProfile> repository, IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<FamilyProfileController>();
            _repository = repository;
            _mapper = mapper;
        }

        [Function("familyprofiles")]
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
                        _logger.LogError("Could not get any family profiles");
                        return await GetErroRespons("Could not get any family profiles", req);
                    }
                    else if (!result.Any())
                    {
                        _logger.LogInformation("No family profiles found");
                        await response.WriteAsJsonAsync(new List<FamilyProfileModel>());
                        return response;
                    }

                    var sorted = result.OrderBy(p => p.Name).ToArray();
                    var models = _mapper.Map<FamilyProfileModel[]>(sorted);
                    await response.WriteAsJsonAsync(models);
                    return response;
                }
                else if (req.Method == "POST")
                {
                    var requestBody = await req.ReadFromJsonAsync<FamilyProfileModel>();
                    if (requestBody == null) return await GetNoContentRespons("No family profile to create", req);

                    var profile = _mapper.Map<FamilyProfile>(requestBody);
                    profile.LastModified = DateTime.UtcNow;

                    var addRes = await _repository.Insert(profile);
                    if (addRes == null)
                    {
                        _logger.LogWarning("Could not create family profile");
                        return await GetErroRespons("Could not create family profile", req);
                    }

                    await response.WriteAsJsonAsync(_mapper.Map<FamilyProfileModel>(addRes));
                    return response;
                }
                else if (req.Method == "PUT")
                {
                    var requestBody = await req.ReadFromJsonAsync<FamilyProfileModel>();
                    if (requestBody == null) return await GetNoContentRespons("No family profile to update", req);

                    var profile = _mapper.Map<FamilyProfile>(requestBody);
                    profile.LastModified = DateTime.UtcNow;

                    var updateRes = await _repository.Update(profile);
                    if (updateRes == null)
                        return await GetErroRespons("Could not update family profile", req);

                    await response.WriteAsJsonAsync(_mapper.Map<FamilyProfileModel>(updateRes));
                    return response;
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong in execution of familyprofiles, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }

        [Function("familyprofile")]
        public async Task<HttpResponseData> RunOne([HttpTrigger(AuthorizationLevel.Anonymous, "get", "delete", Route = "familyprofile/{id}")] HttpRequestData req, string id)
        {
            try
            {
                if (req.Method == "GET")
                {
                    var result = await _repository.Get(id);
                    if (result == null)
                    {
                        _logger.LogInformation($"Could not find family profile with id {id}");
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }

                    var model = _mapper.Map<FamilyProfileModel>(result);
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(model);
                    return response;
                }
                else if (req.Method == "DELETE")
                {
                    // FamilyProfile has no IsActive — hard delete
                    var deleted = await _repository.Delete(id);
                    if (!deleted)
                    {
                        _logger.LogWarning($"Could not delete family profile with id {id}");
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }

                    return req.CreateResponse(HttpStatusCode.OK);
                }

                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                var msg = $"Something went wrong in execution of familyprofile/{id}, method type {req.Method}";
                _logger.LogError(e, msg);
                return await GetErroRespons(msg, req);
            }
        }
    }
}
