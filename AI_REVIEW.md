# Registro de revisión del código generado con IA

Este documento recoge cada vez que el código propuesto por el asistente tuvo que
corregirse: porque yo lo rechacé, porque lo modifiqué, o porque se detectó un
problema después de escribirlo.

El objetivo no es demostrar que la IA se equivoca, sino demostrar el hábito de
**revisar críticamente** lo que genera antes de darlo por bueno.

## Formato de cada entrada

```
### NN - Título corto  (paso N)
- **Qué se generó:** ...
- **Qué problema tenía:** ...
- **Cómo se corrigió:** ...
- **Qué aprendí:** (opcional)
```

---

## Entradas

### 01 - Rama por defecto `master` en lugar de `main`  (paso 1)
- **Qué se generó:** `git init -b main` combinado con `mkdir` en la misma línea.
- **Qué problema tenía:** el comando se ejecutó sobre un repositorio ya
  inicializado, así que git ignoró `--initial-branch` con un aviso y la rama
  quedó como `master`. El aviso se pasó por alto al leer solo el final de la
  salida.
- **Cómo se corrigió:** se comprobó la rama activa con `git branch --show-current`
  y se renombró con `git branch -m master main`.
- **Qué aprendí:** conviene verificar el estado resultante, no solo asumir que el
  comando hizo lo que pedía la bandera.

### 02 - El asistente iba a crear todo el backend de golpe  (paso 2)
- **Qué se generó:** un único comando que creaba la solución, el proyecto de la
  API y el de tests a la vez, ejecutado directamente por el asistente.
- **Qué problema tenía:** el código habría sido correcto, pero yo no habría
  entendido qué hacía cada comando ni por qué. En un proyecto para aprender, que
  la IA lo haga todo sola es un problema aunque el resultado funcione.
- **Cómo se corrigió:** rechacé la ejecución y cambié la forma de trabajar: los
  comandos importantes los ejecuto yo, en tramos pequeños, después de que el
  asistente me explique qué hacen. El paso 2 se hizo en cinco tramos.
- **Qué aprendí:** la IA acelera mucho, pero hay que decidir en qué partes
  conviene delegar y en cuáles no. Lo que no entiendo no lo puedo defender en una
  entrevista ni mantener después.
