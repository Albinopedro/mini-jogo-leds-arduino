# 🎮 Mini Jogo LEDs - Arduino Gaming Platform

Sistema de jogos interativos com matriz 4x4 de LEDs conectada ao Arduino, controlado por aplicação desktop com sistema de autenticação e interface moderna.

## 🚀 Quick Start

### 👤 **Para Jogadores**
1. Digite seu **nome** e **código de acesso** (ex: `AB1234`)
2. Escolha o **jogo desejado**
3. Clique **"🚀 Entrar no Jogo"**
4. Arduino conecta automaticamente!

### 🔧 **Para Administradores**
1. Digite: `ADMIN2024`
2. Acesso completo liberado
3. Configure Arduino e gere códigos de cliente

## 🎯 Jogos Disponíveis

| Jogo | Dificuldade | Descrição | Objetivo |
|------|-------------|-----------|----------|
| 🎯 **Pega-Luz** | ⭐⭐ | Pressione LEDs que acendem rapidamente | Máxima velocidade de reação |
| 🧠 **Sequência Maluca** | ⭐⭐⭐ | Memorize e repita sequências crescentes | Memorização perfeita |
| 🐱 **Gato e Rato** | ⭐⭐ | Persiga o LED piscante pela matriz | Capture 5 vezes em 2 min |
| 🎸 **Guitar Hero LED** | ⭐⭐⭐⭐ | Toque as notas no ritmo da música | Ritmo perfeito |
| 🌧️ **Chuva de Meteoros** | ⭐⭐⭐ | Desvie dos meteoros que caem | Sobreviva 60 segundos |
| 🧩 **Quebra-Cabeça** | ⭐⭐⭐⭐⭐ | Monte padrões específicos | Complete todos os níveis |
| ⚡ **Reação Extrema** | ⭐⭐⭐⭐ | Múltiplos LEDs simultâneos | Reflexos ultra-rápidos |
| 🎲 **Jogo da Sorte** | ⭐ | LEDs aleatórios, escolha o certo | Sorte e intuição |

## 🔐 Sistema de Autenticação

### 👥 **Tipos de Usuários**

**🔧 Administradores**
- Código: `ADMIN2024`
- Acesso completo ao sistema
- Geração de códigos de cliente
- Debug e configurações

**🎮 Clientes/Jogadores**
- Códigos únicos de 6 caracteres (ex: `MX7391`)
- Nome obrigatório para identificação
- Interface simplificada
- Conexão automática

### 🎫 **Códigos de Cliente**
- **Formato**: 2 letras + 4 números
- **Únicos**: Cada código usado apenas 1 vez
- **Geração**: Admin gera 1-10.000 códigos em arquivo `.txt`

## ⌨️ Controles

### 🎮 **Controles Principais**
- **WASD/Setas**: Movimento nos jogos
- **Espaço**: Ação principal
- **Enter**: Confirmar seleções
- **ESC**: Pausar/Voltar

### 🎭 **Efeitos Visuais (F6-F10)**
- **F6**: Matrix Digital
- **F7**: Chuva Colorida
- **F8**: Fogos de Artifício
- **F9**: Ondas Concêntricas
- **F10**: Explosão Central

## 🔧 Hardware Setup

### 📦 **Componentes**
- Arduino Uno R3
- 16x LEDs (4 cores: vermelho, amarelo, verde, azul)
- 16x Resistores 220Ω
- Protoboard e jumpers
- Cabo USB A-B

### 🔌 **Conexões**
```
Linha 0 (VERMELHOS): LEDs 0-3   → Pinos 2-5
Linha 1 (AMARELOS):  LEDs 4-7   → Pinos 6-9
Linha 2 (VERDES):    LEDs 8-11  → Pinos 10-13
Linha 3 (AZUIS):     LEDs 12-15 → Pinos A0-A3
```

### 🏗️ **Layout da Matriz**
```
┌─────────────────────────┐
│  🔴    🔴    🔴    🔴   │  ← Linha 0
│  🟡    🟡    🟡    🟡   │  ← Linha 1
│  🟢    🟢    🟢    🟢   │  ← Linha 2
│  🔵    🔵    🔵    🔵   │  ← Linha 3
└─────────────────────────┘
```

## 💻 Instalação

### 📋 **Requisitos**
- Windows 10/11 x64
- .NET 8.0 Runtime
- Arduino IDE (para upload do firmware)
- Porta USB disponível

### 🚀 **Passos**
1. Clone o repositório
2. Instale dependências: `dotnet restore`
3. Upload do firmware Arduino
4. Execute: `dotnet run`
5. Conecte o Arduino via USB

### 📁 **Estrutura**
```
miniJogo/
├── src/              # Código fonte da aplicação
├── arduino/          # Firmware Arduino
├── assets/           # Sons e recursos
├── data/             # Códigos e estatísticas
└── docs/             # Documentação
```

## 🎵 Sistema de Áudio

- **42 sons únicos** de alta qualidade
- **5 categorias**: Sistema, Jogos, Específicos, Efeitos, Ambiente
- **Feedback sonoro** para cada ação
- **Síntese musical** avançada

## 📊 Sistema de Pontuação

- **Rankings globais** por jogo
- **Estatísticas detalhadas** (tempo, acertos, erros)
- **Histórico completo** de partidas
- **Achievements** e conquistas

## 🔌 Protocolo Arduino

### PC → Arduino
- `1-16`: Acender LED específico
- `L`: Ligar todos os LEDs
- `D`: Desligar todos os LEDs

### Arduino → PC
- `P1-P16`: LED pressionado
- `READY`: Sistema pronto
- `ERROR`: Erro de comunicação

## 🛠️ Troubleshooting

**Arduino não conecta**
- Verifique cabo USB e porta
- Reinstale drivers Arduino
- Teste comunicação serial

**LEDs não funcionam**
- Verifique conexões e resistores
- Teste continuidade dos circuitos
- Confirme upload do firmware

**Áudio não funciona**
- Verifique drivers de áudio
- Teste com outros aplicativos
- Reinstale dependências .NET

## 🎯 Features Técnicas

- **Performance otimizada** com rendering acelerado
- **Arquitetura modular** para fácil extensão
- **Comunicação serial** robusta e confiável
- **Interface responsiva** com Avalonia UI
- **Sistema de logs** detalhado para debug

## 📄 Licença

Este projeto está licenciado sob a MIT License. Consulte o arquivo LICENSE para detalhes.

## 📞 Suporte

- **Issues**: Reporte bugs no GitHub
- **Documentação**: Wiki do projeto
- **Comunidade**: Discord do projeto

---

**🎮 Divirta-se jogando!**