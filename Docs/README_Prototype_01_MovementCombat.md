# NYMARA — Prototype_01_MovementCombat (MVP de movimento e combate)

Cena graybox para validar **controle e combate básico** do Portador do Pacto com o Fio do Limiar.
Unity 6000.6.9f1 · URP 17.6 (2D) · Input System 1.20.1 · Cinemachine 6.6.0.

> Estado deste documento: branch `fix/prototype-01-air-dash-refinement`, criada a partir de `develop` (commit df75303), em 14/11/2026.
> Histórico: `fix/prototype-01-polish` (impulso dos golpes, alvo do inimigo, build) → `fix/prototype-01-air-dash-refinement` (mergulho aéreo e dash).

## Como executar
1. Abra o projeto no Unity **6000.6.9f1**.
2. Abra `Assets/Scenes/Prototypes/Prototype_01_MovementCombat.unity` e clique em **Play**. A câmera (Cinemachine) acompanha o protagonista.
3. Para um build de teste: **File ▸ Build Profiles ▸ Build**. A cena do protótipo é a primeira da lista (índice 0), então o build inicia direto nela. `SampleScene` continua na lista, depois dela.
4. Se precisar recriar a cena e os prefabs: **NYMARA ▸ Prototype ▸ Build Prototype_01_MovementCombat** (só sobrescreve os arquivos gerados por ele, após confirmação).
5. Atalhos de teste, somente em Play Mode: **NYMARA ▸ Prototype ▸ Teste (Play Mode) ▸ Ir para Zona A/B/C**.

## Controles (sem mudanças de mapeamento)
| Ação | Teclado | Gamepad |
|---|---|---|
| Mover | A/D ou ←/→ | Stick esquerdo / D-pad |
| Pular (segurar = mais alto) | Espaço ou ↑ | A / Cross |
| Dash (chão e ar) | Shift | RB / R1 |
| Atacar | J ou botão esquerdo do mouse | X / Square |
| Reiniciar a cena | R | Select |

**Ataque:** no chão o golpe sai ao **soltar** o botão. Um toque curto executa o próximo golpe do combo (3 golpes).
Se segurar pelo menos **0,5 s** e soltar, sai o **ataque carregado** (o corpo fica dourado e pulsa quando a carga está completa).
No ar, o ataque sai ao **pressionar** e é um **mergulho vertical**: o protagonista desce em linha reta e atinge o que estiver logo abaixo, com 1 uso por tempo no ar.
O dash cancela golpes e cargas e **atravessa os Ecos** (não atravessa paredes, chão nem obstáculos). Pular cancela a carga. Sofrer impacto cancela o golpe.
Os bindings ficam em `Assets/Settings/Input/NymaraPrototype.inputactions`. O `InputSystem_Actions` do template não foi alterado.

## Fases visuais de Bren e Prototype 02 (feat/bren-initial-visual)
- **Duas fases, pastas separadas** em `Assets/Art/Prototype/Bren`:
  - `Initial/`: Bren antes da Espada de Arua, sem arma. Hoje só existe o Idle, direcional:
    - `Initial/Idle/Right` e `Initial/Idle/Left`: 8 quadros cada, 256×720 px, PPU 394, pivô nos pés, sem espelhamento.
    - `Initial/Idle/Legacy`: Idle anterior (não direcional), arquivado e sem referências.
  - `Arua_Sword/`: Bren com a espada, usado aqui no Prototype 01 (28 quadros provisórios em Idle, Run, Jump, Fall, Land, Dash, Hurt e `Combat/*`).
  - `Shared/`: referências, conceitos e paletas comuns às duas fases (vazia por enquanto).
- **Prefabs:**
  - `Assets/Prefabs/Prototype/Player.prefab` continua sendo o Bren com espada. Não foi alterado.
  - `Assets/Prefabs/Characters/Bren/Bren_Initial.prefab` é uma variante dele. Troca o sprite e os quadros do `BrenSpriteAnimator` (Idle Right e Idle Left, 8 quadros cada, a 7 FPS, com `directionalSprites` ligado) e deixa o `PlayerCombat` desligado. Com `directionalSprites` o corpo nunca é espelhado: o `BrenSpriteAnimator` compensa no transform visual do corpo o flip de escala feito pelo `PlayerController2D` e escolhe os quadros pela direção do `Facing`. Estados sem arte usam o Idle da direção atual. No Bren com espada a opção fica desligada e nada muda. Física, colliders e movimento são herdados sem mudança.
- **Cena:** `Assets/Scenes/Prototypes/Prototype_02_Bren_Initial.unity` é uma cópia do ambiente do Prototype 01, sem os Ecos, com o Bren inicial.
- **Ferramentas:** menu **NYMARA ▸ Bren** (`BrenPhaseSetup.cs`, `BrenArtPaths.cs`). O passe visual do Prototype 01 agora busca os quadros com espada em `Arua_Sword/` e não mexe na importação de `Initial/`.

## Passe visual provisório (art/prototype-01-visual-pass)
Arte provisória ilustrada (não é pixel art), gerada por código para este protótipo. Não usa imagens de terceiros. Só muda o visual: movimento, combate, IA, vida, dano, colliders e posições não foram alterados.

- **Bren:** sprites em `Assets/Art/Prototype/Bren`, todos olhando para a direita (o flip vem do `PlayerController2D`).
  - São 28 quadros: parado, corrida (6), pulo, ápice, queda, pouso, dash, dano, os 3 golpes (preparação, golpe e recuperação), carga, carga cheia, golpe carregado, preparação do mergulho e mergulho.
  - `BrenSpriteAnimator` escolhe o quadro lendo o estado do controller e do combate. Não usa Animator.
  - O Fio do Limiar só aparece durante os ataques e a carga.
- **Ecos:** em `Assets/Art/Prototype/Ecos` há corpo, camada de brilho das marcas do Intervalo e silhueta de clarão para cada um dos 7 quadros.
  - `EcoVisual` mostra o aviso do golpe (o brilho esquenta e pulsa), o arco do golpe, o clarão branco ao receber dano e o balanço ao ficar atordoado.
  - Também anima os fragmentos flutuantes e faz um anel de luz na libertação.
- **Cenário:** a raiz `ForestArt` da cena tem fundo em 4 camadas com parallax (`ParallaxLayer`), plantas em primeiro plano só abaixo do chão e arte desenhada exatamente nos limites de cada collider.
  - Os blocos originais continuam com o mesmo `BoxCollider2D`, Transform e layer; só o `SpriteRenderer` foi desligado.
- **Efeitos:** `PlayerVfx` e `SimpleFx` usam apenas sprites, sem sistema de partículas.
  - Bren: poeira no pulo, no pouso e no dash, rastro do dash e do mergulho, arcos dos golpes, faíscas de impacto, brilho da carga e impacto do mergulho.
- **Cores dos estados:** viraram tons sutis, para não tingir a arte.
  - `PlayerVisualFeedback`: base branca.
  - `TestEnemyController`: base lilás mais clara.
- **Reaplicar:** menu **NYMARA ▸ Prototype ▸ Aplicar Passe Visual (Prototype 01)**. Esse menu é obrigatório depois de reconstruir a cena pelo Build, porque o builder recria os quadrados graybox.
- **Arquivos:**
  - Scripts em `Assets/Scripts/Visual/*` e `Assets/Scripts/Editor/PrototypeVisualPass.cs`.
  - Arte em `Assets/Art/Prototype/{Bren,Ecos,Forest,FX}`.
  - O gerador da arte (Python) não está no projeto.

## Refinamento: mergulho aéreo e dash (fix/prototype-01-air-dash-refinement)

### Mergulho vertical (ataque aéreo)
- **Antes:** golpe diagonal à frente e abaixo (hitbox em x +0,6 / y −0,6), com a queda limitada a 4 u/s e 70% de controle horizontal durante o golpe.
- **Agora:** ao atacar no ar, o `PlayerCombat` pede `RequestVerticalDive(velocidade, duração)` ao `PlayerController2D`. Enquanto o mergulho dura, o controlador fixa a velocidade em (0, −`aerialDiveSpeed`): sem deslocamento horizontal, mesmo com direcional pressionado.
- O mergulho termina ao tocar o chão ou depois de `aerialDiveMaxDuration`. Depois disso a gravidade e o controle normais voltam; não fica velocidade forçada ativa.
- A hitbox aérea foi movida para baixo do corpo (centro em x 0 / y −1,0, tamanho 1,1 × 1,1) e fica ativa no mínimo pelo tempo `active` e, além disso, enquanto o mergulho durar.
- Continua limitado por `maxAerialAttacksPerAirtime` (1), sem novo botão. Dano, hit stop, recuo e libertação são os mesmos.
- Dash, dano recebido, retorno, perda de controle e o fim do combate (volta para Idle) encerram o mergulho.
- **Animação:** o protótipo não tem Animator nem clipes (é graybox), então não há animação de mergulho. O feedback continua sendo a cor de ataque e o quadro de debug da hitbox.

### Distância do dash (+11%)
- `dashDuration` passou de **0,16 s para 0,18 s**. `dashSpeed` (14), `dashCooldown` (0,2), `postDashSpeedKeep` (0,45) e cargas não mudaram.
- Medido no Editor, no chão, sem direcional depois do dash: **2,552 u → 2,832 u (+11,0%)**, em duas medições de cada valor, para a direita e para a esquerda.
- O contador do dash anda em passos de física de 0,02 s e, por arredondamento de ponto flutuante, 0,16 s rendia **9 passos** e 0,18 s rende **10 passos**. O aumento possível sem mexer na velocidade é de um passo (≈ +11%); 0,17 s cairia nos mesmos 10 passos.

### Dash atravessa inimigos
- Novo campo `dashPassThroughMask` no `PlayerController2D` (no prefab: layer **Enemy**).
- Ao começar o dash, o controlador acrescenta essas layers ao `excludeLayers` do collider do corpo do protagonista e remove-as da máscara que interrompe o dash. Paredes, chão e obstáculos (layer Ground) continuam bloqueando.
- A matriz de colisão do projeto **não** foi alterada, e o collider não é desligado: só deixa de gerar contato com a layer Enemy durante a passagem.
- Ao terminar o dash, a colisão volta assim que o corpo não estiver mais sobreposto a um inimigo, ou no máximo após `dashPassThroughReleaseTimeout` (0,3 s), para a física não expulsar o protagonista de dentro do Eco.
- A colisão é restaurada também ao desativar o componente, ao perder o controle (retorno) e ao teleportar.
- O avanço dos golpes continua parando em inimigos (usa a máscara completa). As hitboxes não usam essa colisão, então atravessar um Eco não causa dano em nenhum dos dois; o Eco continua podendo acertar o protagonista durante o dash (não há invulnerabilidade, como antes).

### Arquivos desta rodada
| Arquivo | Mudança |
|---|---|
| `Assets/Scripts/Player/Movement/PlayerController2D.cs` | Mergulho vertical (`RequestVerticalDive`, `ClearVerticalDive`, `IsDiving`); passagem por inimigos no dash (`dashPassThroughMask`, `dashPassThroughReleaseTimeout`, `IsPassingThroughEnemies`); `dashDuration` 0,18 |
| `Assets/Scripts/Player/Combat/PlayerCombat.cs` | Ataque aéreo usa o mergulho; novos `aerialDiveSpeed` e `aerialDiveMaxDuration`; removido `aerialFallSpeedClamp`; hitbox aérea abaixo do corpo |
| `Assets/Scripts/Editor/PrototypeSceneBuilder.cs` | Preenche `dashPassThroughMask` com a layer Enemy |
| `Assets/Prefabs/Prototype/Player.prefab` | Valores novos acima |

## Rodada anterior: correções de polimento (fix/prototype-01-polish)

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

Layers do protótipo: **Ground (8), Player (9), Enemy (10)**. Enemy×Enemy não colide. Player×Enemy colide: o avanço dos golpes **não atravessa** inimigos; o dash atravessa, só enquanto dura (ver acima).

## Regras de NYMARA respeitadas
- **Inimigo:** ao chegar a estabilidade 0 entra em `Liberated`. Ele para, deixa de causar dano, o "olho" do Intervalo some, há um clarão suave, a cor muda para luz clara, fragmentos de luz sobem e a forma se eleva e se dissolve. Não há sangue, queda nem texto de morte.
- **Protagonista:** com estabilidade 0 a forma se contrai ("absorção pela ruptura"), a tela escurece e ele é reconstruído no ponto de teste com estabilidade cheia. Enquanto ele está desestabilizado, os Ecos deixam de persegui-lo. Ecos ainda aprisionados voltam ao início; Ecos libertados **não** voltam.

## Parâmetros de tuning (Inspector)
| Onde | Campo | Valor atual |
|---|---|---|
| PlayerCombat ▸ cada golpe | `lungeSpeed` × `lungeDuration` | Golpe 1: 2,5 × 0,08 s · Golpe 2: 3 × 0,08 s · Golpe 3: 4,5 × 0,10 s · Carregado: 7 × 0,12 s · Aéreo: sem avanço |
| PlayerCombat ▸ Ataque aéreo (**novo**) | `aerialDiveSpeed`, `aerialDiveMaxDuration` | 16 u/s · 0,4 s |
| PlayerCombat ▸ Ataque aéreo | `aerialMoveMultiplier` | 0,7 (só na recuperação; no mergulho não há controle horizontal) |
| PlayerController2D ▸ Detecção (**novo**) | `dashPassThroughMask` | Enemy |
| PlayerController2D ▸ Dash (**novo**) | `dashPassThroughReleaseTimeout` | 0,3 s |
| PlayerController2D ▸ Impulso de ataque | `attackImpulseEndSpeedKeep` | 0 (para na hora ao terminar) |
| PlayerController2D ▸ Impulso de ataque | `attackImpulseStopsAtObstacles` | ligado |
| PlayerController2D | `maxRunSpeed` / acelerações | 6 · 70/80 (chão) · 45/25 (ar) |
| PlayerController2D | `jumpVelocity`, `baseGravityScale`, `fallGravityMultiplier` | 12 · 3 · 1,6 |
| PlayerController2D | `coyoteTime`, `jumpBufferTime`, `jumpCutMultiplier` | 0,1 · 0,1 · 0,5 |
| PlayerController2D | `dashSpeed`, `dashDuration`, `dashCooldown`, `maxDashCharges` | 14 · **0,18** (antes 0,16) · 0,2 · 1 |
| PlayerCombat | `comboWindow`, `inputBufferTime` | 0,35 · 0,2 |
| PlayerCombat | `chargeThreshold`, `chargeMoveMultiplier` | 0,5 · 0,25 |
| StabilityComponent (Player) | `maxStability`, `invulnerabilityAfterHit` | 5 · 0,8 |
| StabilityComponent (Eco) | `maxStability` | 6 |
| TestEnemyController | `windupTime`, `recoverTime`, `attackDamage`, `detectRange`, `attackRange` | 0,45 · 0,7 · 1 · 6 · 1,4 |

O deslocamento de cada avanço é `lungeSpeed × lungeDuration`: 0,20 u, 0,24 u, 0,45 u e 0,84 u. A duração é arredondada para passos de física de 0,02 s. Movimentação, gravidade e dano não foram alterados nesta rodada.

## Testes

### Rodada air-dash-refinement (14/11/2026, Editor 6000.6.9f1)
Executados em Play Mode por automação de teclado e mouse na tela, com latência de 0,2 a 0,5 s por comando e uma tecla por vez. Alguns valores foram alterados **só dentro da sessão de Play** para isolar o teste (indicado em cada linha); os prefabs não foram alterados por isso.

**Executados e comprovados**
| Teste | Como | Resultado |
|---|---|---|
| Compilação | Console após atualizar os assets e após a sessão de Play | 0 erros e nenhum aviso novo (os avisos de API obsoleta só aparecem quando o Unity recompila) |
| Distância do dash | Posição X no Inspector antes/depois, no chão, sem direcional; `dashDuration` trocado entre 0,16 e 0,18 na mesma sessão | 0,16 s: 2,552 u (duas vezes). 0,18 s: 2,832 u (duas vezes). **+11,0%** |
| Dash contra a parede | Dois dashes seguidos contra a parede esquerda | Parou em X −23,595 (face da parede menos meia largura); não atravessou |
| Dash atravessa o Eco | Protagonista encostado no Eco (bloqueado andando), depois dash; Eco com `detectRange` 0 e `patrolSpeed` 0 só na sessão, para não atacar nem andar | Passou para o outro lado sem o dash ser interrompido |
| Colisão restaurada depois do dash | Andar de volta contra o mesmo Eco | Bloqueado pelo corpo do Eco de novo |
| Mergulho sem deslocamento horizontal | Correr para a direita, pular e atacar; `aerialDiveSpeed` 1,5 e duração 3 s só na sessão, para dar tempo de ler o HUD | HUD: `Falling`, `Vel 0,0 / −1,5`, `Active (Aerial)`; segurar D durante o mergulho não deslocou o protagonista |
| Mergulho atinge inimigo abaixo/ao lado | Pular encostado no Eco parado e atacar | Estabilidade do Eco caiu 1,5 (dano do aéreo) |
| Controle volta após o mergulho | Andar logo depois de aterrissar | Movimento normal |
| Mergulho com valores padrão | Pular e atacar com 16 u/s | Aterrissou em menos de 0,5 s, em `Recovery (Aerial)` |

**Verificados só por leitura de código (não reexecutados)**
- Limite de 1 ataque aéreo por tempo no ar: `maxAerialAttacksPerAirtime` e o reset ao aterrissar não mudaram.
- Liberação da passagem por inimigos quando o dash termina **dentro** de um Eco (sobreposição ou 0,3 s), e restauração no retorno/teleporte/desativação.
- Combo de 3 golpes, carregado e avanço dos golpes: o código só mudou na máscara passada ao teste de obstáculo, que continua incluindo inimigos.

**Pendentes de validação humana**
1. Sensação do mergulho (16 u/s, até 0,4 s) e se a falta de animação atrapalha a leitura.
2. Sensação do dash mais longo (+11%).
3. Dash terminando dentro de um Eco: conferir se não há "empurrão" estranho ao reativar a colisão.
4. Mergulho sobre o Eco: o protagonista pode ficar apoiado na cabeça do Eco (ver Limitações).
5. Regressão completa: combo de 3 golpes, carregado, avanço, cancelamentos, pulo e controle aéreo, estabilidade e retorno, Ecos libertados.
6. Gamepad.

### Rodada polish (anterior)

Os testes em Play Mode foram feitos em 14/11/2026 no Editor 6000.6.9f1, por automação de teclado e mouse na tela. A latência é de 0,2 a 0,5 s por comando e só uma tecla pode ser segurada por vez.

#### Executados e comprovados
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

#### Verificados só parcialmente
- **Ataque aéreo:** o protagonista apareceu no ar com a cor de ataque, mas o texto do HUD não ficou legível na captura. A hitbox aérea não foi medida de novo nesta rodada.
- **Dash aéreo:** o protagonista terminou em cima do degrau depois de pulo + dash, o que é coerente com o dash aéreo, mas não houve captura durante o dash.
- **Impulso dos golpes 2 e 3:** usam o mesmo código do golpe 1 e do carregado, mas não foram medidos isoladamente.

#### Pendentes de avaliação humana
1. **Sensação do impulso:** se 0,20 / 0,24 / 0,45 / 0,84 u são perceptíveis e controláveis com o ritmo real de jogo.
2. **Combo de 3 golpes em sequência ritmada**, com e sem direcional.
3. **Impulso encostado no inimigo e na parede:** conferir se parar no obstáculo fica natural ou se o golpe "trava".
4. **Cancelamentos:** dash no meio do avanço, pulo na recuperação, sofrer dano durante o avanço.
5. **Build de teste:** gerar o build, confirmar que abre no protótipo e que **R** recarrega a cena. O build não foi gerado nesta rodada.
6. **Gamepad:** todos os controles.
7. **Percurso completo** das Zonas A e C com pulo em corrida.
8. **Conforto da câmera** e agressividade do Eco.

## Limitações conhecidas
- **Sem animação de mergulho:** o protótipo não tem Animator nem clipes; o mergulho é só mecânico, com feedback de cor e hitbox de debug.
- **Protagonista sobre o Eco:** o chão é detectado só na layer Ground. Se o protagonista cair em cima de um Eco, ele não conta como "no chão" (não pula e pode usar o mergulho ali), e o mergulho empurra contra o Eco até acabar o tempo (0,4 s). Isso já acontecia antes desta rodada para o pulo; o mergulho apenas herdou.
- **Atalho de pausa nos testes:** `Ctrl+Shift+P` também envia Shift, que é o dash. Para testar com pausa, use o botão de pausa da barra do Editor.
- **Fim de linha:** os scripts alterados e este README estão com CRLF na pasta de trabalho; o Git avisa que converte os `.cs` para LF no commit.
- **Avisos de API obsoleta no Unity 6.6:** `FindObjectsByType(FindObjectsSortMode)` em `PrototypeHUD` e `PlayerRespawn`, e `FindFirstObjectByType` em `PrototypeDebugMenu`. Funcionam, mas o Unity recomenda as variantes novas. Não foram trocados nesta rodada.
- **Botão de ataque "preso" no HUD:** em uma ocasião, logo após o retorno, o HUD mostrou `Ataque [X]` sem nenhuma tecla pressionada. Não gerou ataque e sumiu em seguida. Na rodada air-dash-refinement aconteceu de novo ao clicar na aba **Game** com o mouse: o clique conta como botão de ataque e a soltura não foi registrada, deixando o protagonista em carga até um novo toque em J. Provável efeito do clique na aba (fora da área do jogo); não foi investigado.
- **Degrau da Zona C:** o Eco não sobe o degrau. Se o protagonista ficar do outro lado, o Eco fica em Chase encostado nele.
- O golpe básico sai ao **soltar** o botão. Isso evita ataques duplicados com a carga, mas adiciona a duração do toque como atraso.
- O Eco é agressivo para um primeiro contato: ataca a cada ~1,3 s e o protagonista aguenta 5 impactos.
- A altura do pulo deixa algumas plataformas no limite (Platform_C3, Obstacle_Tall).
- O hit stop usa `Time.timeScale` (global). Não há animações, partículas reais nem áudio; o feedback é por cor e forma.
- O HUD é IMGUI de debug.
- A cena vazia `Assets/Scenes/Prototypes/Prototype_01.unity` continua no projeto.
