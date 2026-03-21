module.exports = {
  type: 'graphviz',
  dot: `digraph lexer_parser {
    rankdir=LR;
    bgcolor="#1e1e2e";
    node [fontname="Consolas" fontsize=10 style=filled shape=box];
    edge [fontname="Segoe UI" fontsize=9 color="#89b4fa"];

    subgraph cluster_input {
      label="입력";
      labeljust=l;
      fontname="Segoe UI"; fontsize=11; fontcolor="#cdd6f4";
      style=dashed; color="#585b70";

      source [label="소스 코드\\n\\"repeated string name = 1;\\"" fillcolor="#45475a" fontcolor="#f9e2af" shape=note];
    }

    subgraph cluster_lexer {
      label="Lexer (문자 → 토큰)";
      labeljust=l;
      fontname="Segoe UI"; fontsize=11; fontcolor="#f5c2e7";
      style=rounded; color="#f5c2e7"; bgcolor="#181825";

      lex_rule1 [label="REPEATED\\n'repeated'" fillcolor="#313244" fontcolor="#f5c2e7"];
      lex_rule2 [label="STRING\\n'string'" fillcolor="#313244" fontcolor="#f5c2e7"];
      lex_rule3 [label="IDENTIFIER\\nLETTER (LETTER|DIGIT)*" fillcolor="#313244" fontcolor="#f5c2e7"];
      lex_rule4 [label="EQ  '='" fillcolor="#313244" fontcolor="#f5c2e7"];
      lex_rule5 [label="INT_LIT\\nDECIMAL_LIT" fillcolor="#313244" fontcolor="#f5c2e7"];
      lex_rule6 [label="SEMI  ';'" fillcolor="#313244" fontcolor="#f5c2e7"];
      lex_ws [label="WS -> skip\\n공백 제거" fillcolor="#45475a" fontcolor="#a6adc8" style="filled,dashed"];
    }

    subgraph cluster_tokens {
      label="토큰 스트림";
      labeljust=l;
      fontname="Segoe UI"; fontsize=11; fontcolor="#a6e3a1";
      style=dashed; color="#585b70";

      tokens [label="REPEATED | STRING | IDENTIFIER(\\"name\\")\\nEQ | INT_LIT(\\"1\\") | SEMI" fillcolor="#313244" fontcolor="#a6e3a1" shape=record];
    }

    subgraph cluster_parser {
      label="Parser (토큰 → Parse Tree)";
      labeljust=l;
      fontname="Segoe UI"; fontsize=11; fontcolor="#89b4fa";
      style=rounded; color="#89b4fa"; bgcolor="#181825";

      parse_field [label="field" fillcolor="#313244" fontcolor="#89b4fa"];
      parse_label [label="fieldLabel\\n→ REPEATED" fillcolor="#313244" fontcolor="#89b4fa"];
      parse_type [label="type_\\n→ STRING" fillcolor="#313244" fontcolor="#89b4fa"];
      parse_name [label="fieldName\\n→ ident → IDENTIFIER" fillcolor="#313244" fontcolor="#89b4fa"];
      parse_num [label="fieldNumber\\n→ intLit → INT_LIT" fillcolor="#313244" fontcolor="#89b4fa"];
    }

    subgraph cluster_output {
      label="출력";
      labeljust=l;
      fontname="Segoe UI"; fontsize=11; fontcolor="#cdd6f4";
      style=dashed; color="#585b70";

      cst [label="Parse Tree (CST)\\n문법 규칙 트리" fillcolor="#45475a" fontcolor="#a6e3a1" shape=note];
    }

    source -> lex_rule1 [label="  문자 매칭  " fontcolor="#f5c2e7"];
    lex_rule1 -> lex_rule2 [style=invis];
    lex_rule2 -> lex_rule3 [style=invis];
    lex_rule3 -> lex_rule4 [style=invis];
    lex_rule4 -> lex_rule5 [style=invis];
    lex_rule5 -> lex_rule6 [style=invis];

    lex_rule6 -> tokens [label="  토큰 생성  " fontcolor="#a6e3a1"];
    tokens -> parse_field [label="  구조 분석  " fontcolor="#89b4fa"];

    parse_field -> parse_label;
    parse_field -> parse_type;
    parse_field -> parse_name;
    parse_field -> parse_num;

    parse_field -> cst [label="  트리 출력  " fontcolor="#a6e3a1"];
  }`
};
