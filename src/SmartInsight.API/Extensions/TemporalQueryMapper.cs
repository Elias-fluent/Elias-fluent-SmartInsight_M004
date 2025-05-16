using System;
using System.Linq;
using SmartInsight.Core.DTOs;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;

namespace SmartInsight.API.Extensions
{
    /// <summary>
    /// Mapper for converting between temporal query DTOs and model classes
    /// </summary>
    public static class TemporalQueryMapper
    {
        /// <summary>
        /// Maps a TemporalQueryDto to a TemporalQuery model
        /// </summary>
        /// <param name="dto">The DTO to map</param>
        /// <returns>The mapped model</returns>
        public static TemporalQuery ToModel(this TemporalQueryDto dto)
        {
            if (dto == null)
            {
                return null;
            }
            
            var result = new TemporalQuery
            {
                AsOfDate = dto.AsOfDate,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                VersionNumber = dto.VersionNumber,
                IncludeDeleted = dto.IncludeDeleted
            };
            
            // Set up the TripleQuery portion
            result.TripleQuery = new TripleQuery
            {
                SubjectId = dto.SubjectId,
                PredicateUri = dto.PredicateUri,
                ObjectId = dto.ObjectId,
                GraphUri = dto.GraphUri,
                Limit = dto.MaxResults,
                Offset = dto.Offset
            };
            
            // Map change types if provided
            if (dto.ChangeTypes != null && dto.ChangeTypes.Any())
            {
                var changeTypesList = dto.ChangeTypes
                    .Where(ct => Enum.TryParse<ChangeType>(ct, true, out _))
                    .Select(ct => Enum.Parse<ChangeType>(ct, true))
                    .ToArray();
                
                if (changeTypesList.Length > 0)
                {
                    result.ChangeTypes = changeTypesList;
                }
            }
            
            return result;
        }
    }
} 