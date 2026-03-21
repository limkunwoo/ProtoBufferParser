// token_pipeline.js — Graphviz DOT scene plugin for Section 1
// 렉싱 파이프라인: 소스 텍스트 → AntlrInputStream → Lexer → CommonTokenStream → Parser

module.exports = {
  type: 'graphviz',
  dot: `digraph token_pipeline {
  rankdir=LR;
  bgcolor="#1e1e2e";
  node [fontname="Consolas" fontsize=10 style=filled shape=box];
  edge [fontname="Segoe UI" fontsize=9];

  subgraph cluster_input {
    label="소스 텍스트";
    labelloc=t;
    fontname="Segoe UI"; fontsize=11; fontcolor="#cba6f7";
    style=dashed; color="#cba6f7";
    bgcolor="#1e1e2e";

    source [label="message Player {\\n  string name = 1;\\n  int32 level = 2;\\n}" fillcolor="#2c3e50" fontcolor="#f5e0dc" color="#cba6f7" shape=note];
  }

  subgraph cluster_lex {
    label="렉싱 단계";
    labelloc=t;
    fontname="Segoe UI"; fontsize=12; fontcolor="#89b4fa";
    style=solid; color="#89b4fa"; penwidth=1.5;
    bgcolor="#1a2535";

    inputStream [label="AntlrInputStream\\n(문자 스트림)" fillcolor="#2c3e50" fontcolor="#89b4fa" color="#89b4fa"];
    lexer [label="Protobuf3Lexer\\n(패턴 매칭)" fillcolor="#2c3e50" fontcolor="#89b4fa" color="#89b4fa" penwidth=2];
    tokenStream [label="CommonTokenStream\\n(토큰 버퍼)" fillcolor="#2c3e50" fontcolor="#89b4fa" color="#89b4fa"];

    inputStream -> lexer [label="문자 읽기" color="#89b4fa" fontcolor="#89b4fa"];
    lexer -> tokenStream [label="토큰 생성" color="#89b4fa" fontcolor="#89b4fa"];
  }

  subgraph cluster_tokens {
    label="토큰 스트림 (14개)";
    labelloc=t;
    fontname="Segoe UI"; fontsize=11; fontcolor="#a9dc76";
    style=dashed; color="#a9dc76";
    bgcolor="#1e1e2e";

    t0 [label="MESSAGE" fillcolor="#1a3a2a" fontcolor="#a9dc76" color="#a9dc76"];
    t1 [label="ID(\\"Player\\")" fillcolor="#1a3a2a" fontcolor="#7ec8e3" color="#a9dc76"];
    t2 [label="LC  \\"{\\""  fillcolor="#1a3a2a" fontcolor="#fab387" color="#a9dc76"];
    t3 [label="STRING" fillcolor="#1a3a2a" fontcolor="#a9dc76" color="#a9dc76"];
    tdots [label="... (10개 더)" fillcolor="#1a3a2a" fontcolor="#555555" color="#555555" style="filled,dashed"];

    t0 -> t1 -> t2 -> t3 -> tdots [color="#a9dc76" style=dashed arrowsize=0.5];
  }

  subgraph cluster_hidden {
    label="히든 채널";
    labelloc=t;
    fontname="Segoe UI"; fontsize=10; fontcolor="#555555";
    style=dashed; color="#555555";
    bgcolor="#1e1e2e";

    ws [label="WS → skip\\n(완전 폐기)" fillcolor="#2c2c2c" fontcolor="#666666" color="#555555"];
    comment [label="COMMENT →\\nchannel(HIDDEN)\\n(보존)" fillcolor="#2c2c2c" fontcolor="#666666" color="#555555"];
  }

  subgraph cluster_parser {
    label="파서";
    labelloc=t;
    fontname="Segoe UI"; fontsize=11; fontcolor="#f38ba8";
    style=dashed; color="#f38ba8";
    bgcolor="#1e1e2e";

    parser [label="Protobuf3Parser\\n(CST 생성)" fillcolor="#2c3e50" fontcolor="#f38ba8" color="#f38ba8" penwidth=2];
  }

  source -> inputStream [label="문자열 전달" color="#cba6f7" fontcolor="#cba6f7" penwidth=1.5];
  tokenStream -> t0 [label="채널 0 토큰" color="#a9dc76" fontcolor="#a9dc76" penwidth=1.5];
  lexer -> ws [label="공백" color="#555555" fontcolor="#555555" style=dashed];
  lexer -> comment [label="주석" color="#555555" fontcolor="#555555" style=dashed];
  tokenStream -> parser [label="토큰 소비" color="#f38ba8" fontcolor="#f38ba8" penwidth=1.8];
}`
};
