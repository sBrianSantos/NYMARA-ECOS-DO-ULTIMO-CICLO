# NYMARA — Prototype_01_MovementCombat (MVP de movimento e combate)

Cena graybox para validar **controle e combate básico** do Portador do Pacto com o Fio do Limiar.
Unity 6000.3.24f1 · URP 2D · Input System 1.20 · Cinemachine 3.1.6.

## Como abrir e jogar
1. Abra `Assets/Scenes/Prototypes/Prototype_01_MovementCombat.unity`.
2. Clique em **Play**. A câmera (Cinemachine) acompanha o protagonista.
3. Se precisar recriar a cena/prefabs: **NYMARA ▸ Prototype ▸ Build Prototype_01_MovementCombat**
   (só sobrescreve os arquivos gerados por ele, após confirmação).
4. Atalhos de teste (somente em Play Mode): **NYMARA ▸ Prototype ▸ Teste (Play Mode) ▸ Ir para Zona A/B/C**.

## Controles
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

Configurações de projeto alteradas pelo builder: layers **Ground (8), Player (9), Enemy (10)**. Na matriz 2D, Enemy×Enemy não colide. Player×Enemy colide: avanço de golpe e dash **não atravessam** inimigos. Atravessar fica para uma possível bênção futura, como o "Passo Cortante".

## Regras de NYMARA respeitadas
- **Inimigo:** ao chegar a estabilidade 0 entra em `Liberated`. Ele para, deixa de causar dano, o "olho" do Intervalo some, há um clarão suave, a cor muda para luz clara, fragmentos de luz sobem e a forma se eleva e se dissolve. Não há sangue, queda nem texto de morte.
- **Protagonista:** com estabilidade 0 a forma se contrai ("absorção pela ruptura"), a tela escurece e ele é reconstruído no ponto de teste com estabilidade cheia. Ecos ainda aprisionados voltam ao início; Ecos libertados **não** voltam.

## Valores de tuning (Inspector) — testar primeiro
| Onde | Campo | Valor atual |
|---|---|---|
| PlayerController2D | `maxRunSpeed` / acelerações | 6 · 70/80 (chão) · 45/25 (ar) |
| PlayerController2D | `jumpVelocity`, `baseGravityScale`, `fallGravityMultiplier` | 12 · 3 · 1,6 (altura máx. ≈ 2,45 u) |
| PlayerController2D | `coyoteTime`, `jumpBufferTime`, `jumpCutMultiplier` | 0,1 · 0,1 · 0,5 |
| PlayerController2D | `dashSpeed`, `dashDuration`, `dashCooldown`, `maxDashCharges` | 14 · 0,16 · 0,2 · 1 |
| PlayerCombat | `comboWindow`, `inputBufferTime` | 0,35 · 0,2 |
| PlayerCombat | `chargeThreshold`, `chargeMoveMultiplier` | 0,5 · 0,25 |
| PlayerCombat | tempos/hitbox/dano/hit stop por golpe | ver `comboAttacks`, `chargedAttack`, `aerialAttack` |
| StabilityComponent (Player) | `maxStability`, `invulnerabilityAfterHit` | 5 · 0,8 |
| StabilityComponent (Eco) | `maxStability` | 6 |
| TestEnemyController | `windupTime`, `recoverTime`, `attackDamage`, `detectRange`, `attackRange` | 0,45 · 0,7 · 1 · 6 · 1,4 |

## Testes realizados em 14/11/2026 e resultados
A compilação foi verificada no Editor: Console com 0 erros e nenhum warning novo. O único warning é "Account API did not become accessible", do pacote AI Assistant, que já existia e não tem relação com o MVP.

Os testes em Play Mode foram feitos por automação de teclado e mouse na tela. A latência foi de cerca de 0,2 a 0,5 s por comando, e não era possível segurar duas teclas ao mesmo tempo. Por isso a **sensação** de controle ainda precisa de uma sessão humana.

| # | Critério | Resultado |
|---|---|---|
| 1 | Cena abre sem erros | ✅ |
| 2 | Movimento esq./dir. e direção visual | ✅ marcador de frente vira corretamente |
| 3 | Pulo funciona e não é infinito | ✅ 3 pulos pressionados no ar → 1 executado (contador do HUD) |
| 4 | Dash chão/ar respeitando colisões | ✅ dash aéreo observado; dash contra a parede não atravessa |
| 5 | Combo básico | ⚠️ golpes 1→2 observados no HUD; os 3 golpes em sequência ritmada não foram validados por causa da latência da automação |
| 6 | Segurar/soltar → carregado; toque → básico | ✅ segurar 0,75 s tirou 3 de estabilidade (carregado); toques curtos geram golpes básicos |
| 7 | Ataque aéreo com hitbox própria | ✅ `AerialHitbox` ativou à frente e abaixo durante a queda (log temporário, já removido) |
| 8 | Acerto no lado correto | ✅ golpes só registram acerto à frente; golpes de costas não acertam |
| 9 | Reação a impacto e libertação | ✅ clarão, recuo, estado Staggered e "LIBERTADO" no HUD* |
| 10 | Libertação sem linguagem de morte | ✅ |
| 11 | Câmera acompanha sem desconforto | ✅ acompanha; o conforto precisa de avaliação humana |
| 12 | Reiniciar repetidamente | ✅ R recarrega; retorno após estabilidade 0 também testado |
| 13 | Sem erros/warnings novos | ✅ |
| 14 | README | ✅ este arquivo |

\* Para validar a libertação com a latência da automação, o `attackDamage` do Eco foi zerado **apenas dentro daquela sessão de Play Mode** (o valor volta sozinho ao sair). O asset não foi alterado.

## Limitações conhecidas
- O golpe básico sai ao **soltar** o botão. Isso evita ataques duplicados com a carga, mas adiciona a duração do toque como atraso (cerca de 50 a 100 ms). Avaliar no playtest.
- O Eco é agressivo para um primeiro contato: ataca a cada ~1,3 s e o protagonista aguenta 5 impactos.
- A altura do pulo (~2,45 u) deixa algumas plataformas no limite (Platform_C3, Obstacle_Tall). O percurso completo das zonas A e C com pulo em corrida **não** foi validado pela automação.
- O hit stop usa `Time.timeScale` (global). Não há animações, partículas reais nem áudio; o feedback é por cor e forma.
- O HUD é IMGUI de debug. Mostra contadores "Pulos lidos/executados" para diagnóstico.
- A cena vazia `Assets/Scenes/Prototypes/Prototype_01.unity` (da tarefa de setup) continua no projeto. Pode ser removida com sua aprovação.

## Próximo incremento recomendado
1. Sessão de playtest humano (teclado e gamepad) e uma rodada de tuning de movimento e agressividade do Eco.
2. Decidir se o golpe básico sai ao pressionar (com a carga começando depois) ou ao soltar, como hoje.
3. Feedback provisório de impacto: partícula e som simples no acerto e na libertação, sem arte final.
