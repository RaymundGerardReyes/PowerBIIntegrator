# Feasibility and Architecture of a C#-Driven Analytics Framework Targeting Power BI via PBIP/PBIR (2026 Status)

## Executive Summary

Power BI's evolution toward project-based metadata (PBIP) and the enhanced report format (PBIR) has made it realistically possible to treat Power BI reports as generated code rather than manually authored binaries. A C#-driven analytics/reporting framework can now compile its own intermediate dashboard model into PBIR report definitions, import them into Fabric/Power BI, and orchestrate embedded experiences, while also generating Excel, Word, and PDF outputs from the same canonical analytics logic. The key feasibility boundary is that Power BI remains a BI engine, not a generic CLR runtime for arbitrary C# executables; integration is achieved via metadata and APIs rather than loading compiled binaries.[^1][^2][^3][^4][^5][^6][^7][^8][^9]

## Background: From PBIX Black Box to PBIP/PBIR

Historically, Power BI reports and semantic models were stored in opaque PBIX files, making automated generation, version control, and external tooling difficult. The introduction of Power BI Projects (PBIP) created a folder-based representation where report and semantic-model definitions live as human-readable text files, enabling Git workflows and programmatic manipulation.[^3][^10][^7][^8]

In PBIP, the semantic model is represented using TMDL (Tabular Model Definition Language), while the report is represented using PBIR, the enhanced report format; this clean separation is central to any compiler-style architecture. PBIR replaces PBIR-Legacy's single report.json file with a structured folder containing separate JSON files for each page, visual, and bookmark, making reports diffable and amenable to code generation.[^11][^6][^7][^8][^9][^1]

## PBIP versus PBIR: Container and Contents

Multiple sources emphasize that PBIP and PBIR play distinct roles: PBIP is the project container/folder, while PBIR is the report-specific format inside that project. PBIP organizes both the report (.Report) and the semantic model (.SemanticModel) and associated metadata into a tree of text files, but PBIR is specifically responsible for the report layer—pages, visuals, bookmarks, and interactions.[^5][^10][^6][^7]

Put differently, PBIP is "the box" and PBIR is "what describes the report inside the box"; the semantic model is stored separately in TMDL files. This distinction aligns with the statement that "PBIP is the overall project container folder, while PBIR is the specific format used inside that folder to store just the report definition," which is consistent with Data Panda's summary that PBIP brings together the PBIR report and TMDL model in one project.[^6][^7][^5]

## Current Status of PBIR (2026)

PBIR has transitioned from preview into mainstream usage, with Microsoft and community sources documenting its adoption roadmap. Microsoft Learn describes report items as supporting both PBIR-Legacy and PBIR formats, with definition.pbir referencing the semantic model and the report definition stored either as a single report.json (legacy) or as a \definition folder (enhanced PBIR).[^12][^13][^2][^14][^15][^1]

Community and blog posts indicate that PBIR became the default report format for new reports in the Power BI Service in January 2026, followed by Power BI Desktop defaults in March 2026, with GA and full parity expected later in 2026. While documentation still labels parts of PBIR as preview and emphasizes enabling it via Desktop preview features, guidance encourages developers to adopt PBIP/PBIR for source-control friendly, agent-friendly workflows.[^2][^15][^7][^8][^12][^11]

## Power BI REST and Fabric APIs Relevant to a Compiler Architecture

The Fabric REST API for report definitions exposes PBIR/PBIR-Legacy as supported formats and describes the component files for a report, including custom visuals, static resources, definition.pbir, report.json or \definition, semanticModelDiagramLayout.json, and mobileState.json. In this structure, definition.pbir holds the reference to the semantic model—either by relative path or remote connection—and is required for report items.[^13][^1]

Separately, the Power BI REST Imports API supports importing .pbix, .json, .xlsx, and .rdl artifacts into workspaces, allowing externally generated reports, semantic models, or paginated reports to be brought into the Power BI environment. Together, these APIs provide the mechanisms for a C# system to either directly submit PBIR-based definitions in Fabric, or to generate artifacts (PBIX/PBIP/Excel/RDL) that Power BI can import and materialize as reports.[^4][^16][^17]

## Power BI Projects and Enhanced Report Format for Code-First Workflows

Microsoft Learn's guidance on "Create a Power BI report in enhanced report format" explains that PBIP projects can save reports using PBIR, with separate text-based components for the semantic model (TMDL) and report (PBIR). Developers are instructed to enable "Store reports using enhanced metadata format (PBIR)" in Desktop preview features, save as a Power BI Project (.pbip), and then inspect the file structure in an editor such as VS Code.[^14][^7][^8][^2]

Endjin and Tabular Editor documentation stress that PBIP/PBIR is specifically intended to make reports source-control friendly, diffable, and manipulable by external tools—precisely the use case of a compiler that generates report definitions from an intermediate representation. PBIR-Legacy is described as storing the entire report in a single JSON file, while PBIR uses a folder structure with separate JSON files per report component, allowing granular versioning and automated generation.[^7][^8][^9][^13]

## Power BI Embedded: Visual Authoring and Custom Layout

On the runtime side, Power BI Embedded's JavaScript client APIs allow host applications to programmatically create visuals, configure their data bindings, and apply layouts within embedded reports. The `createVisual` method accepts a visual type (e.g., `barChart`), an optional layout object, and returns a response object, making it possible to build up a report canvas from code rather than manual authoring.[^18][^19]

Custom report layouts are supported via the `layoutType: models.LayoutType.Custom` setting, with a `customLayout` object specifying page size, canvas scale, and visual layouts (position, size, visibility) for each visual. The documentation explicitly states that embedded hosts can vary the page size and control the size, position, and visibility of visuals, and that layouts can be updated at runtime with `report.updateSettings`.[^19]

## Embedding Reports and Custom Visuals

Microsoft Learn's embedding overview describes how applications can embed entire reports or individual visuals, with support for app-owns-data scenarios (Power BI Embedded) and user-owns-data scenarios. Reports saved with PBIR/PBIP still behave as standard Power BI reports when embedded; the embedding layer is concerned with rendering and interaction, not with how the report was authored.[^20][^18]

Custom visuals can be developed using the Power BI Visuals SDK (TypeScript/JavaScript), and imported into Power BI reports, which then can be embedded like any other report. Enterprise guides emphasize that custom visuals are sandboxed web components that receive data and formatting from the Power BI host and render using libraries such as D3.js, providing a route for highly tailored chart types that still integrate with Power BI's filter and selection mechanisms.[^21][^22][^23]

## Deprecation of Python/R Visuals and Implications for C# Integration

A 2026 article notes that Power BI ends support for Python and R visuals in embedded "app-owns-data" and Publish to Web scenarios as of May 1, 2026; reports containing such visuals will still load but the Python/R tiles render as blank boxes in those contexts. The recommended mitigation is to rebuild charts using DAX measures and native Power BI visuals or custom visuals built with the official SDK.[^24]

This deprecation underscores a broader principle: Power BI's embedded runtime is not meant to execute arbitrary external code (Python, R, or C#) embedded inside visuals; instead, it provides controlled data and formatting to visuals that are compiled to JavaScript/TypeScript and run inside a sandbox. For C#, this means integration should focus on generating metadata and using APIs rather than expecting Power BI to host arbitrary C# executables as its UI engine.[^22][^23][^24]

## External Clarifications on PBIP/PBIR Structure

Community Q&A and blog posts clarify the internal versioning and storage modes of PBIR within PBIP projects. For example, the `definition.pbir` file is always in PBIP format, with a version attribute controlling whether the report definition is stored as PBIR-Legacy (report.json) or as the enhanced PBIR \definition folder; higher versions allow both formats, while version 1.0 requires PBIR-Legacy.[^25][^1][^13][^14]

Guides aimed at developers stress that the `.Report` directory of a PBIP project stores report definitions in PBIR, with separate JSON files for visuals, pages, bookmarks, and other components, while the `.SemanticModel` directory stores the model in TMDL. This reinforces the conceptual split: PBIP is the project-level folder, PBIR is the report-level format, and TMDL is the model-level format.[^8][^6][^7]

## Feasibility of a C# Analytics/UI Framework Targeting PBIR

Given the above capabilities, a C# analytics/UI framework that treats Power BI as a target runtime for its generated reports is technically feasible. The framework can define a canonical analytics model and a dashboard/report intermediate representation (IR) that describe pages, visuals, measures, filters, layout, themes, and relationships, independent of any particular output format.[^7][^8]

From this IR, different backends can generate outputs: a PBIR backend can produce a PBIR-compliant \definition folder plus definition.pbir referencing a semantic model; an Excel backend can generate .xlsx using the Imports API's supported formats; and other backends can generate Word/PDF reports via document-generation libraries. The Power BI generator would then submit PBIR definitions via Fabric APIs or package them into PBIP/PBIX artifacts and import them using the Imports endpoint, effectively acting as a compiler from the canonical IR to Power BI reports.[^17][^1][^4][^5]

## Architectural Boundary: C# as Orchestrator, Not Runtime Inside Power BI

The critical boundary is that Power BI does not expose a supported mechanism to treat arbitrary compiled C# binaries as its internal UI or rendering engine; instead, it renders reports defined in PBIR/TMDL and executes visuals written with the Power BI Visuals SDK. Any architecture that claims "Power BI executes arbitrary C# UI" would not be aligned with Microsoft’s documented capabilities and would risk breaking under platform changes.[^23][^24][^20]

A defensible architecture therefore keeps business logic, analytics, QA, and report specification in C#, but uses Power BI only for the BI layer: semantic models, DAX, filters, relationships, and visuals controlled via PBIR and embedded APIs. C# becomes the orchestrator/compiler that generates PBIR definitions and semantic models, coordinates imports, and embeds the resulting reports inside a custom host UI.[^2][^6][^8][^7]

## Concrete Project Goals Based on Current Capabilities

Based on 2026 documentation and ecosystem status, a concrete and feasible set of goals for such a project includes:

- **Canonical analytics model and dashboard IR**: Define a language or object model in C# that describes metrics, dimensions, measures, relationships, pages, visuals, and layouts in a technology-agnostic way, suitable for multiple output targets.[^8][^7]
- **PBIR code generator**: Implement a backend that converts the dashboard IR into a PBIR-compliant folder structure (\definition, visual/page JSON files) and definition.pbir referencing a semantic model (either local TMDL or remote semantic model by connection), following Fabric's report-definition schema.[^1][^2]
- **Semantic model generator (TMDL or existing models)**: Either generate TMDL files corresponding to the canonical model or connect to existing semantic models via byConnection references, ensuring that the report's bindings match the data model.[^6][^7]
- **Multi-target report backends**: Implement parallel backends for Excel (.xlsx), Word/PDF, and potentially RDL, so that the same canonical analytics logic produces consistent outputs across office documents and Power BI.[^4][^17]
- **Power BI import and publishing pipeline**: Use the Imports API and Fabric REST APIs to import generated artifacts or submit PBIR definitions, creating or updating reports in specific workspaces and assigning them to datasets/semantic models.[^1][^4]
- **Embedded host application**: Build a host web or desktop application (in C#, JavaScript, or hybrid) that embeds Power BI reports and visuals, uses custom layout settings to control page size and visual arrangement, and integrates navigation, branding, authentication, and workflow around the Power BI analytics surface.[^19][^20]
- **Custom visual mapping**: Define a capability matrix that maps the framework's visual types to native Power BI visuals or to custom visuals built with the Power BI Visuals SDK, acknowledging that some highly custom charts require bespoke visuals rather than direct 1:1 mapping.[^18][^21][^22]

These goals are consistent with Microsoft's documented APIs and formats and avoid the unsupported assumption that Power BI can host arbitrary C# binaries as its UI runtime.

## Basis for the Project's Structure and Thesis Claims

The distinction between PBIP and PBIR provides a solid theoretical basis for framing the project as "a C#-driven compiler that targets Power BI's enhanced report format." PBIP, as the project container, can be treated as the outer artifact that holds both the TMDL semantic model and PBIR report definitions; PBIR, as the report definition format, is the direct target of the code generator for pages and visuals.[^5][^6][^7]

This lets the project argue that Power BI reports have become "code"—text-based JSON definitions stored in structured folders—rather than binary black boxes, and that a custom analytics framework can generate and manage these definitions as part of a broader multi-output reporting system. The feasibility analysis can explicitly state the boundary: integration is achieved via metadata generation, REST APIs, and embedding, not via loading compiled C# as Power BI's internal runtime.[^10][^9][^24][^20][^8]

## Conclusion

Current (2026) documentation and ecosystem developments around PBIP, PBIR, TMDL, and Power BI Embedded confirm that a C# analytics/reporting framework which treats Power BI as a target runtime is feasible and well-aligned with Microsoft's intended architecture. PBIP serves as the project container folder, while PBIR is the enhanced report format used inside that folder to store the report definition as a structured set of JSON files, making reports programmatically generatable and source-control friendly.[^5][^6][^7]

The concrete, defensible goal is to build "a C# analytics/report compiler that outputs PBIR-based Power BI reports, alongside Excel, Word, and PDF documents, from a single canonical analytics model," and to orchestrate these outputs via Fabric/Power BI REST APIs and Power BI Embedded, rather than to attempt to turn Power BI into a CLR host for arbitrary compiled C# binaries.[^2][^4][^1]

---

## References

1. [Report definition - Fabric REST APIs - Microsoft Learn](https://learn.microsoft.com/en-us/rest/api/fabric/articles/item-management/definitions/report-definition) - Report definitions can use either PBIR or PBIR-Legacy format, but not both at the same time. The rep...

2. [Create a Power BI report in enhanced report format - Microsoft Learn](https://learn.microsoft.com/en-us/power-bi/developer/embedded/projects-enhanced-report-format) - However, if the report is imported into Fabric using PBIR format, then both features start exporting...

3. [Power BI Desktop projects (PBIP) - Microsoft Learn](https://learn.microsoft.com/en-us/power-bi/developer/projects/projects-overview) - Open a Power BI Project. You can open Power BI Desktop from the Power BI Project folder either by op...

4. [Post Import - Power BI REST APIs - Microsoft Learn](https://learn.microsoft.com/en-us/rest/api/power-bi/imports/post-import) - To import a file, specify the content type multipart/form-data in the request headers and encode the...

5. [Power BI Project (PBIP) | Data Dictionary - Data Panda](https://datapanda.eu/en/dictionary/power-bi-project-pbip) - PBIP is the Power BI project format that splits reports and models into clean folders for easier tea...

6. [PBIP vs PBIR: the distinction nobody's making (and why it quietly ...](https://powerbiskills.ai/blog/pbip-vs-pbir) - PBIP is the structure. The container vs. the contents PBIP — Power BI Project — is not a file format...

7. [The PBIP format in simple terms: why metadata is so important](https://tabulareditor.com/blog/pbip-for-models-and-reports) - In simple terms, it means that you save your Power BI report and semantic model definitions as human...

8. [Why Power BI developers should care about Power BI projects (PBIP)](https://endjin.com/blog/why-power-bi-developers-should-care-about-power-bi-projects) - Power BI Projects are a game changer for teams building reports; offering a source-control friendly ...

9. [Why Power BI developers should care about PBIR - endjin](https://endjin.com/blog/why-power-bi-developers-should-care-about-the-power-bi-enhanced-report-format) - PBIR-Legacy stores the entire report definition in a single JSON file (report.json), while the new P...

10. [Power BI PBIP and PBIR Explained: The End of the PBIX Black Box](https://arinco.com.au/blog/power-bi-pbip-and-pbir-explained-the-end-of-the-pbix-black-box/) - Power BI's new PBIP and PBIR formats replace PBIX with Git-friendly, readable files, unlocking versi...

11. [What is PBIR? Full Guide to Power BI Enhanced Report Format](https://lukasreese.com/2026/03/16/what-is-pbir-full-guide-to-power-bi-enhanced-report-format/) - So, what is PBIR? PBIR — the Power BI Enhanced Report Format — stores every Power BI visual, page, a...

12. [Power BI Desktop to default to PBIR format in March 2026 - LinkedIn](https://www.linkedin.com/posts/prathy_datanovawithprathy-microsoftfabric-powerbi-activity-7437173676812292096-ucB1) - PBIR is about to become the default report format in Power BI Desktop next month. It's already the d...

13. [PBIR format - Microsoft Fabric Community](https://community.fabric.microsoft.com/t5/Desktop/PBIR-format/td-p/4669393) - Report definition must be stored as PBIR-Legacy in the report.json file. 4.0 or higher Report defini...

14. [Power BI Desktop project report folder - Microsoft Learn](https://learn.microsoft.com/en-us/power-bi/developer/projects/projects-report) - Report definition must be stored as PBIR-Legacy in the report.json file. 4.0 or higher, Report defin...

15. [Why PBIR Becoming Default in Jan 2026 is a Milestone - Wix.com](https://jihwanpowerbifabric.wixsite.com/supplychainflow/post/why-pbir-becoming-default-in-jan-2026-is-a-milestone) - PBIR is Power BI Enhanced Report Format. It is the report-specific component that brings code-level ...

16. [Power BI Rest API - Upload Excel file - Microsoft Fabric Community](https://community.fabric.microsoft.com/t5/Developer/Power-BI-Rest-API-Upload-Excel-file/td-p/2754397) - I have created a powershell and I have no problems publishing PBI or RDL, but when I try to publish ...

17. [Upload RDL file(s) to Power BI via API - DEV Community](https://dev.to/kenakamu/upload-rdl-file-s-to-power-bi-via-api-28h) - Use Post Import In Group API to import RDL file from local disk, which creates RDL (or Paginated Rep...

18. [Create a visual in Power BI embedded | Microsoft Learn](https://learn.microsoft.com/en-us/javascript/api/overview/powerbi/create-add-visual) - Learn how to create a visual for a Power BI report in a Power BI embedded analytics application by f...

19. [Report Layout in Power BI Embedded | Microsoft Learn](https://learn.microsoft.com/en-us/javascript/api/overview/powerbi/custom-layout) - Learn how to use a custom Power BI report layout by using the Power BI Client APIs in a Power BI emb...

20. [Power BI Custom Visuals – Data Visualization Tools | Power BI](https://www.microsoft.com/en-us/power-platform/products/power-bi/developers/custom-visualization) - Visualize data your way, with our rich library of fully customizable, open-source data visualization...

21. [Advanced Visualizations: Utilizing Custom Visuals in Power BI ...](https://thereportinghub.com/blog/advanced-visualizations-utilizing-custom-visuals-in-power-bi-embedded) - Custom visuals in Power BI Embedded provide the flexibility to deliver highly tailored insights, tra...

22. [Custom Visuals: Enterprise TypeScript Guide | Power BI Consulting](https://powerbiconsulting.com/blog/power-bi-custom-visuals-development-guide) - Create custom Power BI visuals using TypeScript, D3.js, and the Power BI Visuals SDK for unique data...

23. [GitHub - microsoft/powerbi-visuals-api: Power BI custom ...](https://github.com/microsoft/powerbi-visuals-api) - Simply follow the instructions provided by the bot. You will only need to do this once across all re...

24. [May 2026: What Power BI's Python Deprecation Means for ISVs](https://www.zoho.com/analytics/insightshq/power-bi-embedded-python-visuals-deprecated.html) - Power BI ends support for Python and R visuals in embedded mode on May 1, 2026. Here's what ISVs nee...

25. [Anyone using PBIP or PBIR in Prod? : r/PowerBI - Reddit](https://www.reddit.com/r/PowerBI/comments/1ka6288/anyone_using_pbip_or_pbir_in_prod/) - Hi all,. I want to step up the game and start using Git integration for Power BI. Both PBIP and PBIR...

