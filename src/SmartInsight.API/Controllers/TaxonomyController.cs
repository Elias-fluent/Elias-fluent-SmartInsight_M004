using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInsight.Core.DTOs.Taxonomy;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TaxonomyController : ControllerBase
    {
        private readonly ITaxonomyService _taxonomyService;
        private readonly ITaxonomyVisualizer _taxonomyVisualizer;
        private readonly TaxonomyInheritanceResolver _inheritanceResolver;
        private readonly TaxonomyValidationService _validationService;

        public TaxonomyController(
            ITaxonomyService taxonomyService, 
            ITaxonomyVisualizer taxonomyVisualizer,
            TaxonomyInheritanceResolver inheritanceResolver,
            TaxonomyValidationService validationService)
        {
            _taxonomyService = taxonomyService ?? throw new ArgumentNullException(nameof(taxonomyService));
            _taxonomyVisualizer = taxonomyVisualizer ?? throw new ArgumentNullException(nameof(taxonomyVisualizer));
            _inheritanceResolver = inheritanceResolver ?? throw new ArgumentNullException(nameof(inheritanceResolver));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        }

        #region Node Management

        [HttpGet("nodes/{nodeId}")]
        public async Task<ActionResult<TaxonomyNodeDto>> GetNode(string nodeId, [FromQuery] string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            var node = await _taxonomyService.GetNodeAsync(nodeId, tenantId);
            if (node == null)
            {
                return NotFound();
            }

            return Ok(MapToNodeDto(node));
        }

        [HttpGet("nodes/root")]
        public async Task<ActionResult<IEnumerable<TaxonomyNodeDto>>> GetRootNodes([FromQuery] string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            var nodes = await _taxonomyService.GetRootNodesAsync(tenantId);
            return Ok(nodes.Select(MapToNodeDto));
        }

        [HttpGet("nodes/{parentId}/children")]
        public async Task<ActionResult<IEnumerable<TaxonomyNodeDto>>> GetChildNodes(
            string parentId, 
            [FromQuery] string tenantId, 
            [FromQuery] bool recursive = false)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            var nodes = await _taxonomyService.GetChildNodesAsync(parentId, tenantId, recursive);
            return Ok(nodes.Select(MapToNodeDto));
        }

        [HttpPost("nodes")]
        public async Task<ActionResult<TaxonomyNodeDto>> CreateNode(
            [FromBody] TaxonomyNodeDto nodeDto, 
            [FromQuery] string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var node = MapToNodeModel(nodeDto, tenantId);
                var createdNode = await _taxonomyService.CreateNodeAsync(node, tenantId);
                return CreatedAtAction(nameof(GetNode), 
                    new { nodeId = createdNode.Id, tenantId }, 
                    MapToNodeDto(createdNode));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("nodes/{nodeId}")]
        public async Task<ActionResult<TaxonomyNodeDto>> UpdateNode(
            string nodeId, 
            [FromBody] TaxonomyNodeDto nodeDto, 
            [FromQuery] string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            if (nodeId != nodeDto.Id)
            {
                return BadRequest("Node ID in the URL must match the ID in the payload");
            }

            try
            {
                var existingNode = await _taxonomyService.GetNodeAsync(nodeId, tenantId);
                if (existingNode == null)
                {
                    return NotFound();
                }
                
                var node = MapToNodeModel(nodeDto, tenantId);
                node.Id = nodeId;
                node.Version = existingNode.Version + 1;
                node.CreatedAt = existingNode.CreatedAt;
                
                var updatedNode = await _taxonomyService.UpdateNodeAsync(node, tenantId);
                return Ok(MapToNodeDto(updatedNode));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("nodes/{nodeId}")]
        public async Task<ActionResult> DeleteNode(
            string nodeId, 
            [FromQuery] string tenantId, 
            [FromQuery] bool recursive = false)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var result = await _taxonomyService.DeleteNodeAsync(nodeId, tenantId, recursive);
                if (result)
                {
                    return NoContent();
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Relation Management

        [HttpGet("nodes/{nodeId}/relations")]
        public async Task<ActionResult<IEnumerable<TaxonomyRelationDto>>> GetNodeRelations(
            string nodeId, 
            [FromQuery] string tenantId, 
            [FromQuery] string relationType = null, 
            [FromQuery] bool isOutgoing = true)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            TaxonomyRelationType? relationTypeEnum = null;
            if (!string.IsNullOrEmpty(relationType) && Enum.TryParse<TaxonomyRelationType>(relationType, true, out var parsedType))
            {
                relationTypeEnum = parsedType;
            }

            var relations = await _taxonomyService.GetNodeRelationsAsync(nodeId, tenantId, relationTypeEnum, isOutgoing);
            return Ok(relations.Select(MapToRelationDto));
        }

        [HttpPost("relations")]
        public async Task<ActionResult<TaxonomyRelationDto>> CreateRelation(
            [FromBody] TaxonomyRelationDto relationDto, 
            [FromQuery] string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var relation = MapToRelationModel(relationDto, tenantId);
                var createdRelation = await _taxonomyService.CreateRelationAsync(relation, tenantId);
                return CreatedAtAction(nameof(GetNodeRelations), 
                    new { nodeId = createdRelation.SourceNodeId, tenantId }, 
                    MapToRelationDto(createdRelation));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("relations/{relationId}")]
        public async Task<ActionResult> DeleteRelation(
            string relationId, 
            [FromQuery] string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var result = await _taxonomyService.DeleteRelationAsync(relationId, tenantId);
                if (result)
                {
                    return NoContent();
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Inheritance Rule Management

        [HttpGet("inheritance-rules/{nodeTypeId}")]
        public async Task<ActionResult<IEnumerable<TaxonomyInheritanceRuleDto>>> GetInheritanceRules(
            string nodeTypeId, 
            [FromQuery] string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            var rules = await _taxonomyService.GetInheritanceRulesAsync(nodeTypeId, tenantId);
            return Ok(rules.Select(MapToInheritanceRuleDto));
        }

        [HttpPost("inheritance-rules")]
        public async Task<ActionResult<TaxonomyInheritanceRuleDto>> CreateInheritanceRule(
            [FromBody] TaxonomyInheritanceRuleDto ruleDto, 
            [FromQuery] string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var rule = MapToInheritanceRuleModel(ruleDto, tenantId);
                var createdRule = await _taxonomyService.CreateInheritanceRuleAsync(rule, tenantId);
                return CreatedAtAction(nameof(GetInheritanceRules), 
                    new { nodeTypeId = createdRule.SourceNodeTypeId, tenantId }, 
                    MapToInheritanceRuleDto(createdRule));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("inheritance-rules/{ruleId}")]
        public async Task<ActionResult<TaxonomyInheritanceRuleDto>> UpdateInheritanceRule(
            string ruleId, 
            [FromBody] TaxonomyInheritanceRuleDto ruleDto, 
            [FromQuery] string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            if (ruleId != ruleDto.Id)
            {
                return BadRequest("Rule ID in the URL must match the ID in the payload");
            }

            try
            {
                // Since the ITaxonomyService doesn't have a GetInheritanceRuleAsync method,
                // we'll get all rules for the tenant and filter manually
                var rules = await _taxonomyService.GetInheritanceRulesAsync(ruleDto.SourceNodeTypeId, tenantId);
                var existingRule = rules.FirstOrDefault(r => r.Id == ruleId);
                if (existingRule == null)
                {
                    return NotFound();
                }
                
                var rule = MapToInheritanceRuleModel(ruleDto, tenantId);
                rule.Id = ruleId;
                rule.CreatedAt = existingRule.CreatedAt;
                
                var updatedRule = await _taxonomyService.UpdateInheritanceRuleAsync(rule, tenantId);
                return Ok(MapToInheritanceRuleDto(updatedRule));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("inheritance-rules/{ruleId}")]
        public async Task<ActionResult> DeleteInheritanceRule(
            string ruleId, 
            [FromQuery] string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var result = await _taxonomyService.DeleteInheritanceRuleAsync(ruleId, tenantId);
                if (result)
                {
                    return NoContent();
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Visualization

        [HttpGet("visualize/hierarchy")]
        public async Task<ActionResult<string>> VisualizeHierarchy(
            [FromQuery] string tenantId,
            [FromQuery] string rootNodeId = null,
            [FromQuery] string format = "json",
            [FromQuery] int maxDepth = 0)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var result = await _taxonomyVisualizer.GenerateHierarchyVisualizationAsync(
                    tenantId, rootNodeId, format, maxDepth);
                
                return Content(result, GetContentType(format));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("visualize/graph")]
        public async Task<ActionResult<string>> VisualizeGraph(
            [FromQuery] string tenantId,
            [FromQuery] string centralNodeId = null,
            [FromQuery] string format = "json",
            [FromQuery] string includeRelationTypes = "",
            [FromQuery] int maxDistance = 0)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var result = await _taxonomyVisualizer.GenerateGraphVisualizationAsync(
                    tenantId, centralNodeId, format, includeRelationTypes, maxDistance);
                
                return Content(result, GetContentType(format));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("visualize/treemap")]
        public async Task<ActionResult<string>> VisualizeTreeMap(
            [FromQuery] string tenantId,
            [FromQuery] string rootNodeId = null,
            [FromQuery] string sizeProperty = "count",
            [FromQuery] string colorProperty = "level",
            [FromQuery] string format = "json")
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var result = await _taxonomyVisualizer.GenerateTreeMapVisualizationAsync(
                    tenantId, rootNodeId, sizeProperty, colorProperty, format);
                
                return Content(result, GetContentType(format));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Inheritance Processing

        [HttpPost("inheritance/apply")]
        public async Task<ActionResult> ApplyInheritance(
            [FromQuery] string nodeId, 
            [FromQuery] string tenantId, 
            [FromQuery] bool recursive = true, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            if (string.IsNullOrEmpty(nodeId))
            {
                return BadRequest("Node ID is required");
            }

            try
            {
                var result = await _inheritanceResolver.ApplyInheritanceAsync(nodeId, tenantId, recursive, cancellationToken);
                if (result)
                {
                    return Ok(new { success = true, message = "Inheritance applied successfully" });
                }
                else
                {
                    return BadRequest("Failed to apply inheritance");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("inheritance/apply-upward")]
        public async Task<ActionResult> ApplyUpwardInheritance(
            [FromQuery] string nodeId, 
            [FromQuery] string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            if (string.IsNullOrEmpty(nodeId))
            {
                return BadRequest("Node ID is required");
            }

            try
            {
                var result = await _inheritanceResolver.ApplyUpwardInheritanceAsync(nodeId, tenantId, cancellationToken);
                if (result)
                {
                    return Ok(new { success = true, message = "Upward inheritance applied successfully" });
                }
                else
                {
                    return BadRequest("Failed to apply upward inheritance");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("inheritance/apply-siblings")]
        public async Task<ActionResult> ApplySiblingSharing(
            [FromQuery] string nodeId, 
            [FromQuery] string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            if (string.IsNullOrEmpty(nodeId))
            {
                return BadRequest("Node ID is required");
            }

            try
            {
                var result = await _inheritanceResolver.ApplySiblingSharingAsync(nodeId, tenantId, cancellationToken);
                if (result)
                {
                    return Ok(new { success = true, message = "Sibling sharing applied successfully" });
                }
                else
                {
                    return BadRequest("Failed to apply sibling sharing");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Validation

        [HttpGet("validate/hierarchy")]
        public async Task<ActionResult<IEnumerable<TaxonomyValidationIssue>>> ValidateHierarchy(
            [FromQuery] string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var issues = await _validationService.ValidateHierarchyAsync(tenantId, cancellationToken);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("validate/relations")]
        public async Task<ActionResult<IEnumerable<TaxonomyValidationIssue>>> ValidateRelations(
            [FromQuery] string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var issues = await _validationService.ValidateRelationsAsync(tenantId, cancellationToken);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("validate/inheritance-rules")]
        public async Task<ActionResult<IEnumerable<TaxonomyValidationIssue>>> ValidateInheritanceRules(
            [FromQuery] string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var issues = await _validationService.ValidateInheritanceRulesAsync(tenantId, cancellationToken);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("validate/all")]
        public async Task<ActionResult<object>> ValidateAll(
            [FromQuery] string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }

            try
            {
                var hierarchyIssues = await _validationService.ValidateHierarchyAsync(tenantId, cancellationToken);
                var relationIssues = await _validationService.ValidateRelationsAsync(tenantId, cancellationToken);
                var inheritanceIssues = await _validationService.ValidateInheritanceRulesAsync(tenantId, cancellationToken);
                
                var result = new
                {
                    HierarchyIssues = hierarchyIssues,
                    RelationIssues = relationIssues,
                    InheritanceIssues = inheritanceIssues,
                    TotalIssues = hierarchyIssues.Count + relationIssues.Count + inheritanceIssues.Count,
                    ErrorCount = hierarchyIssues.Count(i => i.Severity == ValidationIssueSeverity.Error) +
                                relationIssues.Count(i => i.Severity == ValidationIssueSeverity.Error) +
                                inheritanceIssues.Count(i => i.Severity == ValidationIssueSeverity.Error),
                    WarningCount = hierarchyIssues.Count(i => i.Severity == ValidationIssueSeverity.Warning) +
                                relationIssues.Count(i => i.Severity == ValidationIssueSeverity.Warning) +
                                inheritanceIssues.Count(i => i.Severity == ValidationIssueSeverity.Warning),
                    InfoCount = hierarchyIssues.Count(i => i.Severity == ValidationIssueSeverity.Info) +
                                relationIssues.Count(i => i.Severity == ValidationIssueSeverity.Info) +
                                inheritanceIssues.Count(i => i.Severity == ValidationIssueSeverity.Info)
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Helper Methods

        private string GetContentType(string format)
        {
            return format.ToLowerInvariant() switch
            {
                "json" => "application/json",
                "xml" => "application/xml",
                "dot" => "text/plain",
                "svg" => "image/svg+xml",
                _ => "application/json"
            };
        }

        private TaxonomyNodeDto MapToNodeDto(TaxonomyNode node)
        {
            if (node == null) return null;

            return new TaxonomyNodeDto
            {
                Id = node.Id,
                Name = node.Name,
                Description = node.Description,
                ParentId = node.ParentId,
                NodeType = node.NodeType.ToString(),
                QualifiedName = node.QualifiedName,
                Namespace = node.Namespace,
                Aliases = node.Aliases,
                Properties = node.Properties,
                IsSystemDefined = node.IsSystemDefined,
                IsActive = node.IsActive
            };
        }

        private TaxonomyNode MapToNodeModel(TaxonomyNodeDto dto, string tenantId)
        {
            if (dto == null) return null;

            TaxonomyNodeType nodeType = TaxonomyNodeType.Class; // Default
            if (Enum.TryParse<TaxonomyNodeType>(dto.NodeType, true, out var parsedType))
            {
                nodeType = parsedType;
            }

            return new TaxonomyNode
            {
                Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
                TenantId = tenantId,
                Name = dto.Name,
                Description = dto.Description,
                ParentId = dto.ParentId,
                NodeType = nodeType,
                QualifiedName = dto.QualifiedName,
                Namespace = dto.Namespace,
                Aliases = dto.Aliases ?? new List<string>(),
                Properties = dto.Properties ?? new Dictionary<string, object>(),
                IsSystemDefined = dto.IsSystemDefined,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };
        }

        private TaxonomyRelationDto MapToRelationDto(TaxonomyRelation relation)
        {
            if (relation == null) return null;

            return new TaxonomyRelationDto
            {
                Id = relation.Id,
                SourceNodeId = relation.SourceNodeId,
                TargetNodeId = relation.TargetNodeId,
                RelationType = relation.RelationType.ToString(),
                Properties = relation.Properties,
                IsSystemDefined = relation.IsSystemDefined,
                IsBidirectional = relation.IsBidirectional,
                Weight = relation.Weight
            };
        }

        private TaxonomyRelation MapToRelationModel(TaxonomyRelationDto dto, string tenantId)
        {
            if (dto == null) return null;

            TaxonomyRelationType relationType = TaxonomyRelationType.RelatedTo; // Default
            if (Enum.TryParse<TaxonomyRelationType>(dto.RelationType, true, out var parsedType))
            {
                relationType = parsedType;
            }

            return new TaxonomyRelation
            {
                Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
                TenantId = tenantId,
                SourceNodeId = dto.SourceNodeId,
                TargetNodeId = dto.TargetNodeId,
                RelationType = relationType,
                Properties = dto.Properties,
                IsSystemDefined = dto.IsSystemDefined,
                IsBidirectional = dto.IsBidirectional,
                Weight = dto.Weight,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private TaxonomyInheritanceRuleDto MapToInheritanceRuleDto(TaxonomyInheritanceRule rule)
        {
            if (rule == null) return null;

            return new TaxonomyInheritanceRuleDto
            {
                Id = rule.Id,
                SourceNodeTypeId = rule.SourceNodeTypeId,
                TargetNodeTypeId = rule.TargetNodeTypeId,
                RuleType = rule.RuleType.ToString(),
                IncludedProperties = rule.IncludedProperties,
                ExcludedProperties = rule.ExcludedProperties,
                Priority = rule.Priority,
                MergeValues = rule.MergeValues,
                IsActive = rule.IsActive,
                Description = rule.Description
            };
        }

        private TaxonomyInheritanceRule MapToInheritanceRuleModel(TaxonomyInheritanceRuleDto dto, string tenantId)
        {
            if (dto == null) return null;

            // Use fully qualified name to resolve ambiguity
            SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models.InheritanceRuleType ruleType =
                SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models.InheritanceRuleType.DownwardInheritance; // Default
            
            if (Enum.TryParse<SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models.InheritanceRuleType>(
                dto.RuleType, true, out var parsedType))
            {
                ruleType = parsedType;
            }

            return new TaxonomyInheritanceRule
            {
                Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
                TenantId = tenantId,
                SourceNodeTypeId = dto.SourceNodeTypeId,
                TargetNodeTypeId = dto.TargetNodeTypeId,
                RuleType = ruleType,
                IncludedProperties = dto.IncludedProperties ?? new List<string>(),
                ExcludedProperties = dto.ExcludedProperties ?? new List<string>(),
                Priority = dto.Priority,
                MergeValues = dto.MergeValues,
                IsActive = dto.IsActive,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        #endregion
    }
} 