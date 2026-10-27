# Estructuras de datos en Shadows

Materia: Estructuras de Datos. Los nueve TP están implementados en la rama `estructuras-de-datos`, que sale de `arreglos-de-luces` y se mantiene aparte del desarrollo del juego. Los arreglos del juego que se hagan en otras ramas hay que traerlos a esta para que no se desfase.

Todo el código propio vive en `Assets/Scripts/Data Structures/`. No se usan `List<T>`, `Stack<T>`, `Queue<T>`, `HashSet<T>` ni `Dictionary<T>` de C# en las mecánicas que cubren los TP.

## Qué hay y dónde se usa

| TP | Estructura | Clases | Uso en el juego |
|----|------------|--------|-----------------|
| 01 | List | `ISimpleList`, `SimpleArrayList`, `SimpleLinkedList` | `DoorHub.Registradas` (puertas del Hub, se lee por índice) y las filas de `PoolDeBloques`. Ambas usan la estática. |
| 02 | Stack | `ISimpleStack`, `SimpleArrayStack`, `SimpleLinkedStack` | `GridManager` apila cada bloque colocado y la tecla Z desapila el último. Estática: apilar y desapilar son O(1) sobre el último lugar. |
| 03 | Queue | `ISimpleQueue`, `SimpleArrayQueue`, `SimpleLinkedQueue` | `NPCDemostrador` consume sus pasos con una `SimpleLinkedQueue`. Dequeue es O(1) en la dinámica y O(n) en el arreglo. |
| 04 | Priority Queue | `ISimplePriorityQueue`, `SimpleArrayPriorityQueue`, `SimpleLinkedPriorityQueue` | Cola de sonidos de `AudioManager` y popups de `PopupManager`. El número más chico sale primero. |
| 05 | Set | `ISimpleSet`, `SimpleArraySet`, `SimpleLinkedSet` | Habilidades desbloqueadas en `AbilityManager`. |
| 06 | Dictionary | `ISimpleDictionary`, `SimpleArrayDictionary`, `SimpleLinkedDictionary` | Habilidades del jugador (`PlayerAbilityController`) y reservas del pool de bloques. |
| 07 | Sorting | `CollectionSort`, `ISimpleList.Sort` (Bubble, Selection, Quick; ascendente o descendente) | `RankingDeTiempos` ordena los mejores tiempos y el popup de cada puerta del Hub muestra el puesto. |
| 08 | BST | `BST<TKey, T>`, `TreeNode<T>` | Base del AVL. Busca por una clave distinta del valor guardado, con un `Func` que se pasa al constructor. |
| 09 | AVL | `AVL<TKey, T>` | Catálogo de bloques de `FabricaDeBloques`, buscado por id. Hereda de `BST` y sobrescribe `Insertar` y `Quitar` para rotar. |

Convenciones que conviene no romper:

- En la cola de prioridad el número más chico sale primero y las prioridades son `int`.
- Leer de una pila o cola vacía tira `InvalidOperationException`.
- El BST y el AVL no admiten valores repetidos: `Add` devuelve `false` si ya estaba.

## Cómo se prueba

### Escena `PruebaTodo`

`Assets/Scenes/Estructuras/PruebaTodo.unity` corre las nueve baterías al dar Play y muestra una tabla con el resultado de cada TP. Un clic en una fila muestra el detalle. El estado actual es 9 de 9 en verde, con 489 comprobaciones:

| TP | Comprobaciones |
|----|----------------|
| 01 List | 86 |
| 02 Stack | 32 |
| 03 Queue | 32 |
| 04 Priority Queue | 52 |
| 05 Set | 72 |
| 06 Dictionary | 87 |
| 07 Sorting | 72 |
| 08 BST | 34 |
| 09 AVL | 22 |

Cada batería es un componente `Prueba*` (por ejemplo `PruebaBST`) que también escribe su resultado en la consola.

### Panel F1 en el juego

En cualquier escena del juego, F1 abre un panel con las estructuras que el juego usa en ese momento: la pila de bloques, la cola de pasos del NPC, los contadores de las colas de prioridad, las habilidades desbloqueadas y registradas, el ranking de tiempos y el árbol del catálogo. El panel solo lee y no tapa los clics. Se crea solo al cargar la primera escena y no existe en un build final.

Algunas secciones solo se llenan si el juego arranca por el flujo normal. Si se abre un nivel suelto, el panel muestra "sin AudioManager" o "sin jugador" porque esos objetos viven en la escena persistente. El ranking necesita el Hub.

## Decisiones y cosas a saber

- **Tiempo guardado.** `SaveSystem.GuardarResultado` ahora conserva el mejor tiempo de cada nivel. Antes lo pisaba con el del último intento.
- **Deshacer con Z.** Devuelve las cargas que costó el bloque. `ShadowBlock.DestruirBloque` devuelve una sola carga aunque el espejo cueste dos, así que Z compensa la diferencia. Cuando la luz destruye un espejo sigue devolviendo una.
- **Sorting.** Estuvo guardado un tiempo solo en un `git stash` y se recuperó a esta rama. El stash no se tocó.
- **`SimpleLinkedList`.** Es simplemente enlazada. En clase se enseña doblemente enlazada, con acceso por el extremo más cercano. Si lo marcan, hay que reescribirla.
- **Set.** Las dos implementaciones son O(n) en todo porque `Add` llama a `Contains`. Se elige por memoria y patrón de uso, no por tiempo.
- **Numeración.** Los nombres de las ramas remotas viejas (`tp02-priority-queue`, `tp03-set-y-arbitraje-de-popups`) usan otra numeración que la de la consigna, en la que el TP02 es Stack y el TP03 es Queue.

## Sin verificar a mano

Todo se corrió dentro de Unity por el MCP, pero hay tres cosas que no se probaron con el teclado y el mouse: Z para deshacer, F1 para abrir el panel y el popup de una puerta real del Hub con el texto del ranking. También falta jugar un nivel completo para confirmar que colocar bloques y recuperar cargas se sienten igual que antes.
