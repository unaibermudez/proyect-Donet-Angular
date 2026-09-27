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

### 03 - Las instrucciones del cambio de dominio daban por hecho cosas que no existían  (entre pasos 2 y 3)
- **Qué se generó:** para pasar de equipos industriales a una tienda de
  tecnología, pedí borrar las migraciones y regenerarlas, renombrar DTOs y
  endpoints, adaptar la entidad `Document` y sustituir los datos de ejemplo.
- **Qué problema tenía:** nada de eso existía todavía: ni migraciones, ni DTOs, ni
  endpoints, ni `Document`, ni *seed*. Ejecutado al pie de la letra, el subagente
  habría creado la primera migración (que es parte del paso 3, y la hago yo) o
  inventado estructuras para poder "renombrarlas".
- **Cómo se corrigió:** antes de lanzar el subagente se contrastaron las
  instrucciones con el estado real del repositorio y se le pasó un inventario
  explícito de lo que había y lo que no. Lo que no existía quedó anotado como
  pendiente en su paso (*seed* en el paso 3, `Document` con relación opcional en
  el paso 7).
- **Qué aprendí:** una instrucción a una IA puede estar mal aunque el objetivo sea
  correcto. Hay que comprobar sus premisas contra el código antes de ejecutarla.

### 04 - Ejemplo falso para justificar las especificaciones anulables  (entre pasos 2 y 3)
- **Qué se generó:** el comentario de `Product.cs` y el documento `02b` justificaban
  que `ScreenInches` fuese anulable con "una consola no tiene pantalla". El
  ejemplo venía de las instrucciones que el asistente principal le dio al
  subagente, y este lo repitió en tres sitios.
- **Qué problema tenía:** es falso. La Nintendo Switch tiene pantalla, y es
  precisamente un producto que entrará en los datos de ejemplo. Además, el error
  escondía el argumento de verdad: la especificación no depende de la categoría,
  sino de cada producto.
- **Cómo se corrigió:** se cambió el ejemplo a "una PS5 no tiene pantalla, una
  Nintendo Switch sí" y se añadió que por eso no se puede decidir por categoría.
  En la misma revisión se matizó otra afirmación exagerada del documento: decía
  que EF Core "traduce peor" las columnas JSONB, cuando en realidad las mapea con
  `ToJson()`; el argumento correcto es que para tres campos no compensa la
  complejidad.
- **Qué aprendí:** revisar lo que genera un subagente también significa revisar lo
  que le pedí. Un error en las instrucciones se multiplica en todos los sitios
  donde el subagente lo aplica.

### 05 - `git commit --amend` sobre un commit que ya estaba en GitHub  (entre pasos 2 y 3)
- **Qué se generó:** para mantener el cambio de dominio en un único commit, el
  asistente añadió las correcciones de la revisión con `git commit --amend`,
  dando por hecho que el commit del subagente no se había subido.
- **Qué problema tenía:** sí estaba subido. El *amend* creó un commit distinto que
  solo habría entrado con `git push --force`, reescribiendo historia pública.
- **Cómo se corrigió:** el push se rechazó. Se inspeccionó el commit remoto con
  `git fetch` y `git log HEAD..origin/main`, se volvió a él con
  `git reset --soft origin/main` (sin perder cambios) y las correcciones fueron en
  un commit nuevo encima. No se usó `--force`.
- **Qué aprendí:** antes de reescribir un commit hay que comprobar si ya es
  público (`git fetch` + `git status`), no suponerlo. Y un push rechazado es una
  señal para investigar, no un obstáculo que saltarse con `--force`.
