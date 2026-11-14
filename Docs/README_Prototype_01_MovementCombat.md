# NYMARA — Prototype_01_MovementCombat (MVP de movimento e combate)

Cena graybox para validar **controle e combate básico** do Portador do Pacto com o Fio do Limiar.
Unity 6000.6.9f1 · URP 17.6 (2D) · Input System 1.20.1 · Cinemachine 6.6.0.

> Estado deste documento: branch `fix/prototype-01-polish`, criada a partir de `develop` (commit 73deaea), em 14/11/2026.

## Como executar
1. Abra o projeto no Unity **6000.6.9f1**.
2. Abra `Assets/Scenes/Prototypes/Prototype_01_MovementCombat.unity` e clique em **Play**. A câmera (Cinemachine) acompanha o protagonista.
3. Para um build de teste: **File ▸ Build Profiles ▸ Build**. A cena do protótipo é a primeira da lista (índice 0), então o build inicia direto nela. `SampleScene` continua na lista, depois dela.
4. Se precisar recriar a cena e os prefabs: **NYMARA ▸ Prototype ▸ Build Prototype_01_MovementCombat** (só sobrescreve os arquivos gerados por ele, após confirmação).
5. Atalhos de teste, somente em Play Mode: **NYMARA ▸ Prototype ▸ Teste (Play Mode) ▸ Ir para Zona A/B/C**.

## Controles (sem mudanças nesta rodada)
| Ação | Teclado | Gamepad |
|---|---|---|
| Mover | A/D ou ←/→ | Stick esquerdo / D-pad |
| Pular (segurar = mais alto) | Espaço ou ↑ | A / Cross |
| Dash (chão e ar) | Shift | RB / R1 |
| Atacar | J ou botão esquerdo do mouse | X / Square |
| Reiniciar a cena | R | Select |

**Ataque:** no chão o golpe sai ao **soltar** o botão. Um toque curto executa o próximo golpe do combo (3 golpes).
Se segurar pelo menos **0,5 s** e soltar, sai o **ataque carregado** (o corpo fica dourado e pulsa quando a carga está completa).
No ar, o ataque sai ao **pressionar** e é o **ataque aéreo descendente**, com 1 uso por tempo no ar.
O dash cancela golpes e cargas. Pular cancela a carga. Sofrer impacto cancela o golpe.
Os bindings ficam em `Assets/Settings/Input/NymaraPrototype.inputactions`. O `InputSystem_Actions` do template não foi alterado.

## Correções desta rodada (fix/prototype-01-polish)

### 1. Referência do alvo do inimigo
- **Causa:** `TestEnemyController` só buscava a estabilidade do alvo no `Awake`, e esse campo não era serializado. Qualquer alvo atribuído depois do `Awake` deixava o inimigo sem saber se o protagonista estava desestabilizado.
- **Correção:** `targetStability` agora é um campo serializado. `SetTarget` grava alvo e estabilidade juntos, e o inimigo resolve a estabilidade de novo se ela estiver vazia ou não pertencer ao alvo atual. O `PrototypeSceneBuilder` passou a usar `SetTarget`, então a cena gerada guarda as duas referências.
- **Observação:** na cena gerada antes da correção, o alvo já era gravado na cena antes do Play, então o `Awake` o encontrava. A correção remove a dependência da ordem; o defeito em si não foi reproduzido em jogo antes da mudança.

### 2. Impulso dos ataques
- **Causa:** `RequestHorizontalVelocity` definia a velocidade por um único passo de física, e no mesmo `FixedUpdate` a desaceleração do chão (80 u/s²) já a reduzia. O avanço do golpe 1 durava cerca de 2 passos e deslocava ~0,07 u.
- **Correção:** o controlador ganhou `RequestHorizontalImpulse(velocidade, duração)` e `ClearHorizontalImpulse()`. Enquanto o impulso dura, a velocidade horizontal é mantida e a aceleração/desaceleração normal não é aplicada. Ao terminar ou ser cancelado, a velocidade é multiplicada por `attackImpulseEndSpeedKeep` (0 por padrão), então não sobra movimento involuntário.
- O impulso é descartado por dash, recuo ao sofrer dano, teleporte/retorno e perda de controle; o combate o cancela ao voltar para Idle.
- Com `attackImpulseStopsAtObstacles` ligado, o impulso para ao encostar em parede ou inimigo (mesma máscara do dash), para não empurrá-los.
- `PlayerController2D` continua sendo a única classe que escreve `Rigidbody2D.linearVelocity`. `PlayerCombat` só usa a API pública.
- `RequestHorizontalVelocity` foi removido; só o `PlayerCombat` o usava.

### 3. Build Settings
`Prototype_01_MovementCombat` foi adicionada como primeira cena habilitada. `SampleScene` vem depois. Nenhuma cena foi removida.

### 4. Identidade do produto
Product Name agora é `NYMARA: ECOS DO ÚLTIMO CICLO`. Company Name e os demais campos não foram alterados.

### Efeitos colaterais de recriar a cena no Unity 6.6
Recriar pelo menu regravou `Player.prefab`, `EcoAprisionado.prefab` e a cena (novos IDs internos, por isso o diff é grande). O Unity também acrescentou dois campos novos em `PrototypeSquare.png.meta` e regravou a matriz de colisão em `Physics2DSettings.asset` em novo formato (mesmos valores).

## Layout da cena
- **Zona A — Movimento** (x −24 a 6): piso, degrau, plataforma alta e plataforma do topo, dois obstáculos, parede esquerda para testar dash contra parede, vão raso (x 6 a 10) do qual dá para sair pulando.
- **Zona B — Combate** (x 10 a 34): arena plana com o Eco Aprisionado.
- **Zona C — Integração** (x 34 a 62): degrau, plataformas, obstáculo alto (exige subir pela plataforma C3) e um segundo Eco com o mesmo prefab.
- **Hierarchy:** Main Camera, Global Light 2D, `Environment/Zone_*`, `Characters/Player`, `Combat/EcoAprisionado_*`, `Checkpoints/PlayerSpawn_MarcoProvisorio`, `UI/(PrototypeHUD, ScreenFade, PrototypeSceneReset)` e `CM_PlayerFollow`.

## Arquivos
| Caminho | Conteúdo |
|---|---|
| `Assets/Scripts/Core/` | `StabilityComponent`, `AttackHitbox`, `HitInfo`, `PrototypeSceneReset` |
| `Assets/Scripts/Player/Movement/` | `PlayerInputReader`, `PlayerController2D` (única fonte que escreve a velocidade) |
| `Assets/Scripts/Player/Combat/` | `PlayerCombat` (combo, carga, aéreo, hit stop) |
| `Assets/Scripts/Player/` | `PlayerRespawn` (absorção → retorno), `PlayerVisualFeedback` |
| `Assets/Scripts/Enemies/` | `TestEnemyController`, `EnemyLiberation`, `LiberationMote` |
| `Assets/Scripts/UI/` | `PrototypeHUD` (IMGUI de debug), `ScreenFade` |
| `Assets/Scripts/Editor/` | `PrototypeSceneBuilder`, `PrototypeDebugMenu` |
| `Assets/Prefabs/Prototype/` | `Player.prefab`, `EcoAprisionado.prefab` |
| `Assets/Art/Prototype/PrototypeSquare.png` | sprite graybox 16×16 |
| `Assets/Materials/Prototype/NoFriction2D` | material 2D sem atrito |
| `Assets/Settings/Input/NymaraPrototype.inputactions` | ações Player e Prototype |

Layers do protótipo: **Ground (8), Player (9), Enemy (10)**. Enemy×Enemy não colide. Player×Enemy colide: avanço de golpe e dash **não atravessam** inimigos.

## Regras de NYMARA respeitadas
- **Inimigo:** ao chegar a estabilidade 0 entra em `Liberated`. Ele para, deixa de causar dano, o "olho" do Intervalo some, há um clarão suave, a cor muda para luz clara, fragmentos de luz sobem e a forma se eleva e se dissolve. Não há sangue, queda nem texto de morte.
- **Protagonista:** com estabilidade 0 a forma se contrai ("absorção pela ruptura"), a tela escurece e ele é reconstruído no ponto de teste com estabilidade cheia. Enquanto ele está desestabilizado, os Ecos deixam de persegui-lo. Ecos ainda aprisionados voltam ao início; Ecos libertados **não** voltam.

## Parâmetros de tuning (Inspector)
| Onde | Campo | Valor atual |
|---|---|---|
| PlayerCombat ▸ cada golpe | `lungeSpeed` × `lungeDuration` (**novo**) | Golpe 1: 2,5 × 0,08 s · Golpe 2: 3 × 0,08 s · Golpe 3: 4,5 × 0,10 s · Carregado: 7 × 0,12 s · Aéreo: sem avanço |
| PlayerController2D ▸ Impulso de ataque (**novo**) | `attackImpulseEndSpeedKeep` | 0 (para na hora ao terminar) |
| PlayerController2D ▸ Impulso de ataque (**novo**) | `attackImpulseStopsAtObstacles` | ligado |
| PlayerController2D | `maxRunSpeed` / acelerações | 6 · 70/80 (chão) · 45/25 (ar) |
| PlayerController2D | `jumpVelocity`, `baseGravityScale`, `fallGravityMultiplier` | 12 · 3 · 1,6 |
| PlayerController2D | `coyoteTime`, `jumpBufferTime`, `jumpCutMultiplier` | 0,1 · 0,1 · 0,5 |
| PlayerController2D | `dashSpeed`, `dashDuration`, `dashCooldown`, `maxDashCharges` | 14 · 0,16 · 0,2 · 1 |
| PlayerCombat | `comboWindow`, `inputBufferTime` | 0,35 · 0,2 |
| PlayerCombat | `chargeThreshold`, `chargeMoveMultiplier` | 0,5 · 0,25 |
| StabilityComponent (Player) | `maxStability`, `invulnerabilityAfterHit` | 5 · 0,8 |
| StabilityComponent (Eco) | `maxStability` | 6 |
| TestEnemyController | `windupTime`, `recoverTime`, `attackDamage`, `detectRange`, `attackRange` | 0,45 · 0,7 · 1 · 6 · 1,4 |

O deslocamento de cada avanço é `lungeSpeed × lungeDuration`: 0,20 u, 0,24 u, 0,45 u e 0,84 u. A duração é arredondada para passos de física de 0,02 s. Movimentação, gravidade e dano não foram alterados nesta rodada.

## Testes

Os testes em Play Mode foram feitos em 14/11/2026 no Editor 6000.6.9f1, por automação de teclado e mouse na tela. A latência é de 0,2 a 0,5 s por comando e só uma tecla pode ser segurada por vez.

### Executados e comprovados
| Critério | Como foi verificado | Resultado |
|---|---|---|
| Compilação | Console após recompilar | 0 erros. 5 avisos de API obsoleta (ver Limitações), que já existiam desde a atualização para o Unity 6.6 |
| Cena abre sem erros de referência | Cena recriada pelo menu e Play | Console sem erros; no Inspector do Eco, `Target` e `Target Stability` apontam para o Player |
| Referência sobrevive à recriação | Arquivo da cena após "Recriar" | `target` e `targetStability` gravados nos dois Ecos |
| Inimigo deixa de perseguir no retorno | Protagonista desestabilizado ao lado do Eco da Zona C, com a absorção alongada para 6 s só naquela sessão de Play | Eco passou de Recover para Patrol e se afastou enquanto o protagonista estava absorvido ao lado dele |
| Impulso perceptível | Distância do protagonista ao marcador de spawn, antes e depois do golpe | Golpe 1: ~0,19 u (esperado 0,20). Carregado: ~0,84 u (esperado 0,84) |
| Impulso respeita a direção | Carregado virado para a esquerda | Deslocou ~0,84 u para a esquerda |
| Movimento normal volta | HUD após os golpes | Velocidade 0,0 e combate Idle; andar e pular funcionaram em seguida |
| Dash no chão | Deslocamento na Zona A | Funciona |
| Pulo não é infinito | 3 toques seguidos, contador do HUD | 3 lidos, 1 executado |
| Ataque carregado | Segurar J ~0,75 s | Executou; tirou estabilidade do Eco |
| Retorno do protagonista | Estabilidade 0 em três ocasiões | Absorção, fade, reposição no spawn com 5/5 |
| Eco libertado não volta | Eco 1 libertado, depois protagonista desestabilizado pelo Eco 2 | Após o retorno: Eco 1 continua "LIBERTADO", Eco 2 voltou a 6/6 |
| Lista de build | Janela Build Profiles | Protótipo no índice 0, SampleScene no índice 1 |
| Product Name | Janela Player Settings | `NYMARA: ECOS DO ÚLTIMO CICLO`; Company Name `DefaultCompany` |
| R reinicia no Editor | R após os testes | Cena recarregou (HUD zerado, Eco 1 de volta a 6/6) |

Para libertar o Eco 1 com a latência da automação, o `attackDamage` dele foi zerado **apenas naquela sessão de Play**. Os valores de Play Mode não ficam gravados: os prefabs continuam com `attackDamage` 1 e `absorbDuration` 0,6.

### Verificados só parcialmente
- **Ataque aéreo:** o protagonista apareceu no ar com a cor de ataque, mas o texto do HUD não ficou legível na captura. A hitbox aérea não foi medida de novo nesta rodada.
- **Dash aéreo:** o protagonista terminou em cima do degrau depois de pulo + dash, o que é coerente com o dash aéreo, mas não houve captura durante o dash.
- **Impulso dos golpes 2 e 3:** usam o mesmo código do golpe 1 e do carregado, mas não foram medidos isoladamente.

### Pendentes de avaliação humana
1. **Sensação do impulso:** se 0,20 / 0,24 / 0,45 / 0,84 u são perceptíveis e controláveis com o ritmo real de jogo.
2. **Combo de 3 golpes em sequência ritmada**, com e sem direcional.
3. **Impulso encostado no inimigo e na parede:** conferir se parar no obstáculo fica natural ou se o golpe "trava".
4. **Cancelamentos:** dash no meio do avanço, pulo na recuperação, sofrer dano durante o avanço.
5. **Build de teste:** gerar o build, confirmar que abre no protótipo e que **R** recarrega a cena. O build não foi gerado nesta rodada.
6. **Gamepad:** todos os controles.
7. **Percurso completo** das Zonas A e C com pulo em corrida.
8. **Conforto da câmera** e agressividade do Eco.

## Limitações conhecidas
- **Avisos de API obsoleta no Unity 6.6:** `FindObjectsByType(FindObjectsSortMode)` em `PrototypeHUD` e `PlayerRespawn`, e `FindFirstObjectByType` em `PrototypeDebugMenu`. Funcionam, mas o Unity recomenda as variantes novas. Não foram trocados nesta rodada.
- **Botão de ataque "preso" no HUD:** em uma ocasião, logo após o retorno, o HUD mostrou `Ataque [X]` sem nenhuma tecla pressionada. Não gerou ataque e sumiu em seguida. Pode ser efeito do clique sintético da automação; não foi investigado.
- **Degrau da Zona C:** o Eco não sobe o degrau. Se o protagonista ficar do outro lado, o Eco fica em Chase encostado nele.
- O golpe básico sai ao **soltar** o botão. Isso evita ataques duplicados com a carga, mas adiciona a duração do toque como atraso.
- O Eco é agressivo para um primeiro contato: ataca a cada ~1,3 s e o protagonista aguenta 5 impactos.
- A altura do pulo deixa algumas plataformas no limite (Platform_C3, Obstacle_Tall).
- O hit stop usa `Time.timeScale` (global). Não há animações, partículas reais nem áudio; o feedback é por cor e forma.
- O HUD é IMGUI de debug.
- A cena vazia `Assets/Scenes/Prototypes/Prototype_01.unity` continua no projeto.
