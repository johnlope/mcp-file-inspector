# ChangeSet Manifest Template for LLM Code Review

Este documento describe la estructura del archivo `pr-manifest.template.json` ubicado en [Templates/pr-manifest.template.json](Templates/pr-manifest.template.json).

## Propósito

Servir como contrato de entrada ligero y optimizado para modelos de lenguaje (Azure Foundry, Copilot, OpenAI) en revisiones automatizadas de merges e integración de cambios entre equipos (commit inicial vs commit final), combinándose con el servidor MCP `mcp-file-inspector`.

## Estructura de Claves

1. **`changeSet`**:
   - Metadatos clave del corte de integración (`team`, `repository`, `baseCommit`, `targetCommit`, `description`, `commits`).
   - Da el contexto del corte y los límites exactos de Git a comparar.

2. **`summary`**:
   - Métricas cuantitativas del corte (`totalFilesChanged`, `totalAdditions`, `totalDeletions`, `modulesChanged`).
   - Da visibilidad inmediata del tamaño del cambio y de los módulos o capas impactadas, útil para telemetría del pipeline y para orientar la amplitud de la revisión del modelo.

3. **`files`**:
   - Cada archivo incluye `path`, `module`, `status` (`modified`, `added`, `deleted`, `renamed`), `language`, métricas de líneas y el `patch` recortado con contexto.
   - Si un archivo requiere mayor contexto (ej. definición de tipos, métodos llamados), el LLM recurre a las herramientas MCP (`ReadFileAsync`, `GetFileOutline`).

4. **`reviewDirectives`**:
   - Directivas o instrucciones de enfoque (`focusAreas`, `instructionsForLlm`) indicándole explícitamente cuándo apoyarse en el MCP `mcp-file-inspector`.
