# Estructuras de datos en Shadows

Materia: Estructuras de Datos. Los diez TP están implementados en la rama `estructuras-de-datos`, que sale de `arreglos-de-luces` y se mantiene aparte del desarrollo del juego. Los arreglos del juego que se hagan en otras ramas hay que traerlos a esta para que no se desfase.

Todo el código propio vive en `Assets/Scripts/Data Structures/`. No se usan `List<T>`, `Stack<T>`, `Queue<T>`, `HashSet<T>` ni `Dictionary<T>` de C# en las mecánicas que cubren los TP.

## Cómo bajarlo

Repo: https://github.com/Lyed01/Shadows. Todo lo de estructuras está en la rama `estructuras-de-datos`, no en `main`.

Si todavía no tenés el proyecto:

```
git clone https://github.com/Lyed01/Shadows.git
cd Shadows
git switch estructuras-de-datos
```

Si ya lo tenés clonado:

```
git fetch origin
git switch estructuras-de-datos
git pull
```

Para volver a traer cambios más adelante alcanza con `git pull` parado en esa rama. Si tenés cambios sin commitear en archivos que la rama también modificó, git se niega a cambiar de rama: commiteá o guardá los cambios con `git stash` antes.

El proyecto se abre con Unity 6000.3.17f1. La primera vez tarda en importar, porque la carpeta `Library` no se versiona. Para comprobar que anda, abrí `Assets/Scenes/Estructuras/PruebaTodo.unity` y dale Play: tiene que mostrar 9 de 9 en verde (el TP10 se prueba jugando, ver más abajo). Para jugarlo, dale Play desde `MainMenu` y entrá por "Jugar": abre el índice de niveles de demostración.

Para pushear cambios hace falta ser colaborador del repo. Si no lo sos, hacé un fork, pusheá ahí y abrí un pull request hacia `estructuras-de-datos`.

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
| 08 | BST | `BST<TKey, T>`, `TreeNode<T>` | Es la base del AVL y el juego no lo usa solo. Guarda los valores ordenados y busca por una clave distinta del valor (por ejemplo, un id de texto), con un `Func` que se pasa al constructor. Recorridos `InOrder`, `PreOrder` y `PostOrder`, más `Min`, `Max`, `Height` y `Dibujar`. |
| 09 | AVL | `AVL<TKey, T>` | Catálogo de bloques de `FabricaDeBloques`, buscado por id. Hereda de `BST` y sobrescribe `Insertar` y `Quitar` para rebalancear con rotaciones, así que buscar, agregar y quitar son O(log n) siempre. |
| 10 | Graph y Dijkstra | `Graph<T>`, `Edge<T>`, `IPathfindingSolver<T>`, `DijkstraSolver<T>` | `GridManager.ConstruirGrafo()` arma un grafo con las celdas libres de la grilla (cada bloque colocado saca una) y `NPCPerseguidor` busca con Dijkstra el camino más corto hasta el jugador. El grafo es una lista de adyacencia genérica: un `SimpleArrayDictionary` de nodo a `SimpleLinkedList` de aristas. Dijkstra usa `SimpleLinkedPriorityQueue` y `SimpleArraySet`. |

Convenciones que conviene no romper:

- En la cola de prioridad el número más chico sale primero y las prioridades son `int`.
- Leer de una pila o cola vacía tira `InvalidOperationException`.
- El BST y el AVL no admiten valores repetidos: `Add` devuelve `false` si ya estaba.

## Cómo funcionan el BST y el AVL

En un BST cada nodo tiene un hijo izquierdo con valores menores y uno derecho con valores mayores. Buscar es bajar por un solo camino, comparando en cada nodo, y cuesta lo que mide el árbol. Si los valores entran ya ordenados, el árbol se estira hacia un lado, queda como una lista y todo pasa a O(n).

El AVL arregla eso. Después de cada alta y cada baja revisa, en cada nodo del camino que recorrió, la altura de sus dos hijos. Si difieren en más de 1, rota. Hay cuatro casos:

| Desbalance | Arreglo |
|------------|---------|
| izquierda-izquierda | una rotación a la derecha |
| derecha-derecha | una rotación a la izquierda |
| izquierda-derecha | rotación izquierda del hijo y después derecha |
| derecha-izquierda | rotación derecha del hijo y después izquierda |

Cada nodo guarda su altura, así que `Height` cuesta O(1). `EstaBalanceado()` recorre el árbol y comprueba que ningún nodo se pase de 1 y que las alturas guardadas sean las reales; `PruebaAVL` lo usa después de insertar y quitar.

En `FabricaDeBloques` el árbol se carga una vez con los prefabs de bloque y después solo se consulta por id con `TryGet`. El panel F1 muestra cuántos tipos hay, la altura y si está balanceado.

## Cómo se prueba

### Niveles de demostración

"Jugar" en el menú abre `IndiceDemostracion`, con un botón por nivel. Son copias de `Nivel1`, `Nivel2` y `Nivel3` (en `Assets/Scenes/Demostracion/`) y se juegan con los controles del juego:

| Nivel | Qué muestra |
|-------|-------------|
| 1 | Noxel ejecuta su rutina desde una cola (TP03) y entrega la primera habilidad (TP05, con su aviso en la cola de prioridad del TP04). Con la habilidad se puede probar la pila de bloques, el pool y el catálogo AVL. |
| 2 | Un guía repite su rutina solo. Arranca con sombra y espejo. |
| 3 | Un perseguidor usa Dijkstra (TP10) para seguirte y se puede encerrar con bloques. Si te alcanza, volvés al inicio sin morir. Llegar a la salida guarda el tiempo en el ranking, que se ordena con `CollectionSort` (TP07). |

A la derecha, el panel de relato muestra una fila por estructura con lo último que hizo y, debajo, los últimos eventos. Los avisos salen del código real del juego a través de `Narrador`. F1 oculta el panel y F2 vuelve al índice. Las habilidades de cada nivel se restauran al salir, y el ranking de las demos usa ids propios, así que no toca el progreso real.

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

### Panel F1 fuera de las demos

En cualquier otra escena del juego, F1 abre un panel con las estructuras que el juego usa en ese momento: la pila de bloques, la cola de pasos del NPC, los contadores de las colas de prioridad, las habilidades desbloqueadas y registradas, el ranking de tiempos y el árbol del catálogo. El panel solo lee y no tapa los clics. Se crea solo al cargar la primera escena y no existe en un build final.

Algunas secciones solo se llenan si el juego arranca por el flujo normal. Si se abre un nivel suelto, el panel muestra "sin AudioManager" o "sin jugador" porque esos objetos viven en la escena persistente. El ranking necesita el Hub (las demos arman sus propias puertas).

## Decisiones y cosas a saber

- **Tiempo guardado.** `SaveSystem.GuardarResultado` ahora conserva el mejor tiempo de cada nivel. Antes lo pisaba con el del último intento.
- **Deshacer con Z.** Devuelve las cargas que costó el bloque. `ShadowBlock.DestruirBloque` devuelve una sola carga aunque el espejo cueste dos, así que Z compensa la diferencia. Cuando la luz destruye un espejo sigue devolviendo una.
- **Sorting.** Estuvo guardado un tiempo solo en un `git stash` y se recuperó a esta rama. El stash no se tocó.
- **`SimpleLinkedList`.** Es simplemente enlazada. En clase se enseña doblemente enlazada, con acceso por el extremo más cercano. Si lo marcan, hay que reescribirla.
- **Set.** Las dos implementaciones son O(n) en todo porque `Add` llama a `Contains`. Se elige por memoria y patrón de uso, no por tiempo.
- **Numeración.** Los nombres de las ramas remotas viejas (`tp02-priority-queue`, `tp03-set-y-arbitraje-de-popups`) usan otra numeración que la de la consigna, en la que el TP02 es Stack, el TP03 es Queue y el TP10 es Graph y Dijkstra.

## Sin verificar a mano

Todo se corrió dentro de Unity por el MCP, sin teclado ni mouse reales. Falta jugar a mano los tres niveles de demostración de punta a punta: que el relato aparezca al hacer cada acción, que el perseguidor deje llegar a la salida y que el ranking se llene al terminar. También quedan sin probar con el teclado Z para deshacer, F1 y F2, y el popup de una puerta real del Hub con el texto del ranking.
