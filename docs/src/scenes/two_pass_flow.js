// two_pass_flow.js — Graphviz DOT scene plugin for Section 5
// 2-Pass 파싱: Pass 1 심볼 수집 → Rewind → Pass 2 CST 생성

module.exports = {
  type: 'graphviz',
  dot: `digraph two_pass {
  rankdir=TB;
  bgcolor="#1e1e2e";
  node [fontname="Consolas" fontsize=10 style=filled shape=box];
  edge [fontname="Segoe UI" fontsize=9];

  subgraph cluster_entry {
    label="진입점";
    labelloc=t;
    fontname="Segoe UI"; fontsize=11; fontcolor="#cba6f7";
    style=dashed; color="#cba6f7";
    bgcolor="#1e1e2e";

    twoPassParse [label="twoPassParse()\\ngrammar entry" fillcolor="#2c3e50" fontcolor="#cba6f7" color="#cba6f7" penwidth=2];
  }

  subgraph cluster_rewind {
    label="DoRewind() — 2-Pass 핵심";
    labelloc=t;
    fontname="Segoe UI"; fontsize=12; fontcolor="#89b4fa";
    style=solid; color="#89b4fa"; penwidth=2;
    bgcolor="#1a2535";

    seek0a [label="Seek(0)\\n토큰 스트림 처음으로" fillcolor="#2c3e50" fontcolor="#89b4fa" color="#89b4fa"];
    newParser [label="new Protobuf3Parser()\\n별도 인스턴스 생성" fillcolor="#2c3e50" fontcolor="#89b4fa" color="#89b4fa"];
    firstPass [label="_isFirstPass = true\\n모든 프레디킷 → true\\n에러 리스너 제거" fillcolor="#1a3a2a" fontcolor="#a9dc76" color="#a9dc76"];
    pass1parse [label="proto() 호출\\n(Pass 1 — 심볼 수집)" fillcolor="#1a3a2a" fontcolor="#a9dc76" color="#a9dc76" penwidth=2];

    seek0a -> newParser [color="#89b4fa"];
    newParser -> firstPass [color="#89b4fa"];
    firstPass -> pass1parse [color="#a9dc76"];
  }

  subgraph cluster_symbols {
    label="심볼 테이블";
    labelloc=t;
    fontname="Segoe UI"; fontsize=11; fontcolor="#fab387";
    style=dashed; color="#fab387";
    bgcolor="#1e1e2e";

    doMsgName [label="doMessageNameDef\\n→ _messageTypes.Add()" fillcolor="#2c3e50" fontcolor="#fab387" color="#fab387"];
    doEnumName [label="doEnumNameDef\\n→ _enumTypes.Add()" fillcolor="#2c3e50" fontcolor="#fab387" color="#fab387"];
    symTable [label="_messageTypes:\\n  {Player, Inventory, ...}\\n_enumTypes:\\n  {Status, Rarity, ...}" fillcolor="#2c3e50" fontcolor="#f5e0dc" color="#fab387" penwidth=2];

    doMsgName -> symTable [color="#fab387"];
    doEnumName -> symTable [color="#fab387"];
  }

  copySymbol [label="CopySymbolTableFrom()\\nPass 1 → 현재 파서로 복사" fillcolor="#2c3e50" fontcolor="#89b4fa" color="#89b4fa"];
  seek0b [label="Seek(0)\\n토큰 스트림 다시 처음으로" fillcolor="#2c3e50" fontcolor="#89b4fa" color="#89b4fa"];

  subgraph cluster_pass2 {
    label="Pass 2 — 실제 파싱";
    labelloc=t;
    fontname="Segoe UI"; fontsize=12; fontcolor="#f38ba8";
    style=solid; color="#f38ba8"; penwidth=1.5;
    bgcolor="#1e1e2e";

    pass2parse [label="proto() 호출\\n(Pass 2 — CST 생성)" fillcolor="#2c3e50" fontcolor="#f38ba8" color="#f38ba8" penwidth=2];
    predicates [label="IsMessageType_()\\nIsEnumType_()\\n→ 심볼 테이블 검사" fillcolor="#2c3e50" fontcolor="#f38ba8" color="#f38ba8"];
    cst [label="CST\\n(Parse Tree)" fillcolor="#1a2a3a" fontcolor="#7ec8e3" color="#f38ba8" penwidth=2 shape=folder];

    pass2parse -> predicates [label="type_ 분기" color="#f38ba8" fontcolor="#f38ba8" style=dashed];
    pass2parse -> cst [label="결과" color="#f38ba8" fontcolor="#f38ba8" penwidth=1.8];
  }

  twoPassParse -> seek0a [label="DoRewind() 액션 실행" color="#cba6f7" fontcolor="#cba6f7" penwidth=1.5];
  pass1parse -> doMsgName [label="epsilon 규칙" color="#fab387" fontcolor="#fab387" style=dashed];
  pass1parse -> doEnumName [label="epsilon 규칙" color="#fab387" fontcolor="#fab387" style=dashed];
  symTable -> copySymbol [color="#fab387" penwidth=1.5];
  copySymbol -> seek0b [color="#89b4fa"];
  seek0b -> pass2parse [label="Pass 2 시작" color="#f38ba8" fontcolor="#f38ba8" penwidth=1.8];
  symTable -> predicates [label="참조" color="#fab387" fontcolor="#fab387" style=dashed];
}`
};
