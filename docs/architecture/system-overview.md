# System Overview

Canonical analytics model (C#) -> Dashboard IR -> parallel backends:
PDF / Excel / Word / Power BI (PBIR + TMDL inside PBIP) -> Fabric publish ->
Power BI Embedded surface rendered inside the React frontend, with a custom
layout engine controlling page size and per-visual position/size/visibility.
