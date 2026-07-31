### User Story1

**As a** DataEngine API consumer
**I want to** retrieve Submodels and Submodel Descriptors using efficient and consistent pagination
**So that** large digital twin datasets can be processed without requiring DataEngine to load and generate all Submodels before applying pagination

---

### Acceptance Criteria
- [ ] DataEngine shall avoid requesting all Shell metadata from the Plugin when a cursor or limit is provided by the consumer.
- [ ] Plugin metadata/shell endpoints shall support pagination using aasId as a cursor.
- [ ] DataEngine shall be able to continue pagination correctly when the client provides a submodelId cursor.
- [ ] The pagination implementation shall preserve the logical ordering of generated Submodels within a Shell Template.
- [ ]  The implementation shall ensure that no Submodels are skipped or duplicated across pages.
- [ ] Solutions for cursor translation between submodelId and aasId shall be documented and evaluated.
- [ ]  Edge cases involving partially processed Shells shall be handled consistently.
- [ ] The selected approach shall be documented before implementation begins.

---

### Concept

Current implementation of both:

GET /submodels
GET /submodel-descriptors

does not leverage pagination during metadata retrieval.
Current flow:

1. DataEngine requests all metadata/shells from Plugin.
2. For every Shell:

- Resolve Shell Template using ShellTemplateMappingRule.
- Request Shell Template from Template Registry.
- Generate Submodel IDs based on Shell Template configuration.

3. After generating all Submodel IDs:

- Apply existing logic used by:
    - Get Submodel by Id
   - Get Submodel Descriptor by Id

4. Finally return paginated response.

This approach works for small datasets but becomes expensive when thousands of products/shells exist.

Desired Improvement
Introduce pagination as early as possible in the processing pipeline.
**Instead of:**

All Shells
    ↓
Generate All Submodels
    ↓
Apply Pagination

**Use:**

Paginated Shell Retrieval
    ↓
Generate Required Submodels
    ↓
Apply Pagination again (bcz we don't know which shell have how many submodel template ) 
    ↓
Return Page

**Key Design Challenge**

Plugin pagination is based on: `aasId`
while DataEngine pagination is exposed using: `submodelId`
A mechanism is required to map: `submodelId -> aasId`

to determine from which Shell Plugin pagination should continue

---

### Additional Notes

**Important Edge Case**

Assume:

```
Product1
 ├─ Nameplate
 ├─ TechnicalData
 ├─ ContactInformation
 └─ CarbonFootprint

Product2
 ├─ Nameplate
 ├─ TechnicalData
 ├─ ContactInformation
 └─ CarbonFootprint
```
Consumer requests: `limit=12cursor=<Product1-TechnicalData>`

Naive implementation:

1. Resolve cursor to Product1 AAS.
2. Send Product1 AAS as cursor to Plugin.
3. Plugin returns Product2.

Result: `Returned Item = Product2 Nameplate`
Expected result: `Returned Item = Product1 ContactInformation`

because pagination should continue within the same Shell before moving to the next Shell.

---

1. This PBI should initially focus on design and architecture evaluation before implementation.
2. A technical proposal should be produced describing:

-  Cursor structure.
-  Ordering guarantees.
-  Plugin API changes.
- Backward compatibility considerations.
- Performance impact.

3.Final implementation should be done only after the pagination concept has been agreed upon.
4. Solution must work consistently for both:  GET /submodels , GET /submodel-descriptors
5. Consider scenarios where Multiple Shell Templates exist.