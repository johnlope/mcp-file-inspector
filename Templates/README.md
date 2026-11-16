# PR Manifest Template for LLM Code Review

Este documento describe la estructura del archivo `pr-manifest.template.json` ubicado en [Templates/pr-manifest.template.json](Templates/pr-manifest.template.json).

## Propósito

Servir como contrato de entrada ligero y optimizado para modelos de lenguaje (Azure Foundry, Copilot, OpenAI) en revisiones automatizadas de Pull Requests, combinándose con el servidor MCP `mcp-file-inspector`.

## Estructura de Claves

1. **`pullRequest`**:
   - Metadatos clave del PR (`title`, `description`, `author`, `team`, `baseCommit`, `targetCommit`).
   - Da el contexto de negocio fundamental para que el LLM entienda la intención funcional.

2. **`summary`**:
   - Estadísticas rápidas (`totalFilesChanged`, `hasDatabaseChanges`, `hasBreakingChangesCandidate`).
   - Permite al LLM activar pautas de revisión especializadas (ej. seguridad SQL, transacciones, compatibilidad binaria).

3. **`files`**:
   - Cada archivo incluye `path`, `module`, `status` (`modified`, `added`, `deleted`, `renamed`), `language`, métricas de líneas y el `patch` recortado con contexto.
   - Si un archivo requiere mayor contexto (ej. definición de tipos, métodos llamados), el LLM recurre a las herramientas MCP (`ReadFileAsync`).

4. **`reviewDirectives`**:
   - Directivas o instrucciones de enfoque (`focusAreas`, `instructionsForLlm`) indicándole explícitamente cuándo apoyarse en el MCP `mcp-file-inspector`.
