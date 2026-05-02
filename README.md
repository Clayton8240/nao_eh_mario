# Blue Bunny Blaster 🐰💨 (BBB)

> Antigamente "Não É Mário (M1)" — agora rebatizado pra **BBB: Blue Bunny Blaster**
> depois que achei um pack de sprites do Kenney com um coelhinho azul que caiu como
> uma luva. Mesmo joguinho, com cara nova e mais conteúdo.

Trabalho de Desenvolvimento de Jogos. Plataforma 2D side-scroller na Unity 6.
Você é um coelho azul que pula de plataforma em plataforma, junta moedas, pisa
em slimes/caranguejos e tenta chegar na bandeira no fim de cada fase. **3 fases
no total**, com dificuldade crescente.

## ✅ O que ta funcionando

- [x] **Loop completo**: Menu → Jogo → Game Over / Vitória
- [x] **Persistência de Score**: score na sessão + recorde salvo em disco (PlayerPrefs)
- [x] **Feedback ao Jogador**: HUD (score/recorde/moedas/vidas/fase) + SFX (pulo, moeda,
      dano, stomp, vitória, click) + popup "+N" flutuante
- [x] **Side scrolling** estilo Mario (escopo do trabalho)
- [x] **3 fases** com layouts diferentes e progressão de dificuldade
- [x] **Sprites de verdade** (Kenney Pixel Line Platformer, CC0)
- [x] **Revisão de código**: bugs corrigidos e código morto removido

## 🎮 Como rodar (passo a passo)

> ⚠️ Precisa do **Unity 6000.4.3f1** instalado (qualquer versão que abra o projeto serve, mas eu testei nessa).

### 1. Abrir o projeto

1. Abra o **Unity Hub**.
2. Clique em **Add → Add project from disk**.
3. Selecione a pasta do projeto (essa pasta aqui).
4. Clique no projeto na lista pra abrir. (Primeira vez demora uns minutinhos pra importar.)

### 2. Abrir a cena

No painel **Project** (canto inferior esquerdo do editor), navegue até:

```text
Assets → Scenes → SampleScene
```

Dê **duplo clique** em `SampleScene`.

### 3. Configurar a cena (única coisa manual!)

1. Na **Hierarchy** (painel à esquerda no topo), apague tudo se quiser deixar limpo (não é obrigatório, o script reaproveita o que já existir).
2. Clique com o **botão direito na Hierarchy** → **Create Empty**.
3. Renomeie esse novo objeto pra `Bootstrap` (ou qualquer nome, tanto faz).
4. Com ele selecionado, no **Inspector** (painel à direita) clique em **Add Component**.
5. Digite `Game Bootstrap` e clique no script que aparecer.
6. Aperte `Ctrl + S` pra salvar a cena.

### 4. Apertar Play

Clique no botão ▶ lá em cima no centro do editor. O menu vai aparecer e é só clicar em **JOGAR**.

## 🎮 Controles

| Ação | Teclas |
| --- | --- |
| Mover | `A` `D` ou setas `←` `→` |
| Pular | `Espaço`, `W` ou seta `↑` |

## 📊 Sistema de pontuação

| Ação | Pontos |
| --- | --- |
| Pegar moeda 🪙 | +10 |
| Pisar em inimigo 👾 | +50 |
| Bônus de fase intermediária 🚩 | +50 por vida restante |
| Bônus de vitória final 🏆 | +100 por vida restante |

O recorde fica salvo entre sessões (não some quando fecha o jogo).

## 🗺️ As 3 fases

| Fase | Nome | Característica |
| --- | --- | --- |
| 1 | **Floresta Calma** | Tutorial. Curta, 1 inimigo só, plataformas tranquilas |
| 2 | **Caverna Saltitante** | Mais buracos, plataformas em alturas variadas, 3 inimigos |
| 3 | **Pulo Final** | Bem mais difícil — segmentos curtos, exige precisão, 6 inimigos |

## 🧠 Como funciona (arquitetura)

Decidi montar tudo por código, sem ficar arrastando coisa no editor. O único componente que precisa ser adicionado manualmente é o `GameBootstrap` — ele constrói o resto da cena em runtime (player, plataformas, moedas, inimigos, UI, câmera, sons).

Estrutura dos scripts (em [Assets/Scripts/](Assets/Scripts/)):

| Arquivo | O que faz |
| --- | --- |
| [GameBootstrap.cs](Assets/Scripts/GameBootstrap.cs) | Monta a cena inteira. **É o ponto de entrada.** |
| [GameManager.cs](Assets/Scripts/GameManager.cs) | Singleton. Score, vidas, recorde, estado, fase atual. |
| [LevelLibrary.cs](Assets/Scripts/LevelLibrary.cs) | Definição das 3 fases (plataformas/moedas/inimigos). |
| [SpriteLibrary.cs](Assets/Scripts/SpriteLibrary.cs) | Carrega o tilemap do Kenney e fatia em sprites. |
| [SfxPlayer.cs](Assets/Scripts/SfxPlayer.cs) | Toca os SFX (gerados por código com onda senoidal). |
| [PlayerController2D.cs](Assets/Scripts/PlayerController2D.cs) | Movimento e pulo do coelho. |
| [EnemyPatrol.cs](Assets/Scripts/EnemyPatrol.cs) | IA do inimigo (patrulha + leva stomp). |
| [Coin.cs](Assets/Scripts/Coin.cs) | Moeda coletável. |
| [Goal.cs](Assets/Scripts/Goal.cs) | Bandeira de fim de fase (avança ou termina). |
| [KillZone.cs](Assets/Scripts/KillZone.cs) | Zona de morte (cair fora do mapa). |
| [CameraFollow2D.cs](Assets/Scripts/CameraFollow2D.cs) | Câmera lateral suave. |
| [UIController.cs](Assets/Scripts/UIController.cs) | Constrói Menu, HUD, Game Over e Vitória por código. |
| [ScorePopup.cs](Assets/Scripts/ScorePopup.cs) | Aquele "+10" amarelo que sobe e some. |

> 💡 O namespace dos scripts continua sendo `NaoEMario` por dentro pra não quebrar
> nada — só o que o jogador vê é "Blue Bunny Blaster". Foi mais barato fazer rebrand
> só na UI/README do que renomear tudo (o git ia se confundir todo).

### 🔍 Revisão de código (o que foi corrigido)

Passei por todos os scripts procurando bugs, código duplicado e código morto:

| Tipo | Onde | Problema | Correção |
| --- | --- | --- | --- |
| Bug | `SpriteLibrary.cs` | `TILE_PLATFORM_MID = 4` era igual a `TILE_GROUND_GRASS = 4` → chão e plataformas usavam o mesmo sprite | Mudou para índice `24` |
| Bug | `GameBootstrap.cs` | `GoToMenu()` não cancelava o `Invoke(Respawn)` pendente → collider do player podia reativar durante o menu | Adicionado `CancelInvoke()` antes de desativar o player |
| Código morto | `GameManager.cs` | Método `Win()` existia só pra chamar `CompleteLevel()`, mas ninguém mais chamava `Win()` | Método removido |
| Código morto | `EnemyPatrol.cs` | Campo `_col` atribuído no `Awake` mas nunca usado em lugar nenhum | Campo removido |
| Código morto | `SpriteLibrary.cs` | 8 constantes definidas mas sem nenhuma referência no código | Constantes removidas |

Padrões de projeto que usei:

- **Singleton** no `GameManager` e `SfxPlayer` (pra ter acesso global fácil).
- **Observer** com `event System.Action` pra UI atualizar quando score/vidas/estado/fase mudam (sem polling).
- **State Machine** simples no `GameManager` (Menu → Playing → GameOver/Victory).
- **Data-driven levels**: os layouts ficam em `LevelLibrary` como dados puros, o
  `GameBootstrap` só lê e desenha. Adicionar uma fase 4 é só inserir mais um item no array.

## 🐛 Se der ruim

| Problema | Solução |
| --- | --- |
| Tela toda azul, nada acontece ao apertar Play | Você esqueceu de adicionar o componente `GameBootstrap` na cena |
| Sprites aparecem como quadrados magenta | O Unity ainda não importou `Assets/Resources/bbb_tilemap.png` — espera o reimport ou faça **Assets → Reimport All** |
| Sprites borrados (não pixel art) | No Unity, clica em `bbb_tilemap.png`, no Inspector seta **Filter Mode = Point** + **Compression = None**. (O código também força isso em runtime.) |
| Erro `InputSystem` not found | Vai em **Window → Package Manager** e confirma que `Input System` ta instalado |
| Nada de som | Pode ser o sistema do PC mesmo. No editor, confirma se o ícone de som no Game View não tá mutado |
| Botões do menu não respondem ao clique | Talvez tem dois `EventSystem` na cena. Apaga um |
| `Both Input Systems disabled` | **Edit → Project Settings → Player → Active Input Handling → Both** e reinicia o editor |

## 🚧 Próximos passos

- [ ] Animações de andar/pular do coelho (já tem o sprite walk no tilemap, falta trocar entre frames)
- [ ] Música de fundo
- [ ] Inimigos com comportamentos diferentes (abelha voa, caranguejo é mais rápido, etc)
- [ ] Power-ups
- [ ] Tela de pause
- [ ] Editor visual de fases (no lugar do hardcode em `LevelLibrary`)

## 📝 Créditos

- Engine: **Unity 6** (com Universal Render Pipeline)
- Input: **Unity New Input System**
- Sprites: **Kenney Pixel Line Platformer** ([kenney.nl](https://kenney.nl)) — licença CC0
- Sons: gerados em runtime com onda senoidal (matemática que vimos em sinais 😄).
- Código: feito do zero por mim como trabalho da disciplina.
