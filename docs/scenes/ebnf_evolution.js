module.exports = {
  type: 'graphviz',
  dot: `digraph ebnf_evolution {
    rankdir=TB;
    bgcolor="#1e1e2e";
    node [fontname="Consolas" fontsize=11 style=filled shape=box];
    edge [fontname="Segoe UI" fontsize=9 color="#89b4fa"];

    subgraph cluster_bnf {
      label="BNF (1960s)";
      labeljust=l;
      fontname="Segoe UI"; fontsize=12; fontcolor="#cdd6f4";
      style=dashed; color="#585b70";

      bnf_rule [label="<rule> ::= <alt1> | <alt2>" fillcolor="#45475a" fontcolor="#cdd6f4"];
      bnf_recur [label="반복은 재귀로만 표현\\n<list> ::= <item> | <item> <list>" fillcolor="#45475a" fontcolor="#f9e2af"];
      bnf_limit [label="선택적 요소 = 빈 대안 추가\\n<opt> ::= <elem> | (empty)" fillcolor="#45475a" fontcolor="#f38ba8"];
    }

    subgraph cluster_ebnf {
      label="EBNF";
      labeljust=l;
      fontname="Segoe UI"; fontsize=12; fontcolor="#cdd6f4";
      style=dashed; color="#585b70";

      ebnf_alt [label="대안: A | B" fillcolor="#313244" fontcolor="#a6e3a1"];
      ebnf_opt [label="선택적: A?" fillcolor="#313244" fontcolor="#a6e3a1"];
      ebnf_star [label="0회 이상: A*" fillcolor="#313244" fontcolor="#a6e3a1"];
      ebnf_plus [label="1회 이상: A+" fillcolor="#313244" fontcolor="#a6e3a1"];
      ebnf_group [label="그룹: (A B)*" fillcolor="#313244" fontcolor="#a6e3a1"];
    }

    subgraph cluster_antlr {
      label="ANTLR4 g4";
      labeljust=l;
      fontname="Segoe UI"; fontsize=12; fontcolor="#cdd6f4";
      style=dashed; color="#585b70";

      antlr_parser [label="Parser Rules (소문자)\\nproto, messageDef, field" fillcolor="#1e1e2e" fontcolor="#89b4fa" penwidth=2 color="#89b4fa"];
      antlr_lexer [label="Lexer Rules (대문자)\\nMESSAGE, SEMI, INT_LIT" fillcolor="#1e1e2e" fontcolor="#f5c2e7" penwidth=2 color="#f5c2e7"];
      antlr_pred [label="Semantic Predicates\\n{ this.IsNotKeyword() }?" fillcolor="#1e1e2e" fontcolor="#fab387" penwidth=2 color="#fab387"];
    }

    bnf_rule -> ebnf_alt [label="  확장  " fontcolor="#89b4fa"];
    bnf_recur -> ebnf_star [label="  대체  " fontcolor="#89b4fa"];
    bnf_limit -> ebnf_opt [label="  대체  " fontcolor="#89b4fa"];
    ebnf_alt -> antlr_parser [label="  채택  " fontcolor="#89b4fa"];
    ebnf_star -> antlr_parser [style=invis];
    ebnf_group -> antlr_lexer [style=invis];
    antlr_parser -> antlr_pred [style=invis];
  }`
};
