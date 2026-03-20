module.exports = {
  type: 'graphviz',
  dot: `digraph g4_structure {
    rankdir=TB;
    bgcolor="#1e1e2e";
    node [fontname="Consolas" fontsize=10 style=filled shape=box];
    edge [fontname="Segoe UI" fontsize=9 color="#89b4fa"];

    subgraph cluster_file {
      label="Protobuf3.g4 파일 구조";
      labeljust=l;
      fontname="Segoe UI"; fontsize=13; fontcolor="#cdd6f4";
      style=rounded; color="#585b70"; bgcolor="#181825";

      grammar_decl [label="grammar Protobuf3;" fillcolor="#89b4fa" fontcolor="#1e1e2e" shape=box];

      options_block [label="options {\\n    superClass = Protobuf3ParserBase;\\n}" fillcolor="#45475a" fontcolor="#cdd6f4"];

      subgraph cluster_parser {
        label="Parser Rules (소문자)";
        labeljust=l;
        fontname="Segoe UI"; fontsize=11; fontcolor="#89b4fa";
        style=dashed; color="#89b4fa"; bgcolor="#1e1e2e";

        pr_proto [label="proto" fillcolor="#313244" fontcolor="#89b4fa"];
        pr_syntax [label="syntax" fillcolor="#313244" fontcolor="#89b4fa"];
        pr_topdef [label="topLevelDef" fillcolor="#313244" fontcolor="#89b4fa"];
        pr_msgdef [label="messageDef" fillcolor="#313244" fontcolor="#89b4fa"];
        pr_enumdef [label="enumDef" fillcolor="#313244" fontcolor="#89b4fa"];
        pr_field [label="field" fillcolor="#313244" fontcolor="#89b4fa"];
        pr_type [label="type_" fillcolor="#313244" fontcolor="#89b4fa"];
        pr_ident [label="ident" fillcolor="#313244" fontcolor="#89b4fa"];
      }

      subgraph cluster_lexer {
        label="Lexer Rules (대문자)";
        labeljust=l;
        fontname="Segoe UI"; fontsize=11; fontcolor="#f5c2e7";
        style=dashed; color="#f5c2e7"; bgcolor="#1e1e2e";

        lr_kw [label="키워드 토큰\\nMESSAGE ENUM\\nSYNTAX IMPORT" fillcolor="#313244" fontcolor="#f5c2e7"];
        lr_sym [label="기호 토큰\\nSEMI EQ LP RP\\nLC RC LB RB" fillcolor="#313244" fontcolor="#f5c2e7"];
        lr_lit [label="리터럴 토큰\\nINT_LIT STR_LIT\\nFLOAT_LIT BOOL_LIT" fillcolor="#313244" fontcolor="#f5c2e7"];
        lr_id [label="IDENTIFIER" fillcolor="#313244" fontcolor="#f5c2e7"];

        subgraph cluster_fragment {
          label="fragment (헬퍼)";
          labeljust=l;
          fontname="Segoe UI"; fontsize=10; fontcolor="#a6adc8";
          style=dotted; color="#a6adc8";

          frag [label="DECIMAL_DIGIT\\nHEX_DIGIT\\nLETTER\\nCHAR_VALUE" fillcolor="#45475a" fontcolor="#a6adc8"];
        }
      }

      subgraph cluster_skip {
        label="Skip / Channel";
        labeljust=l;
        fontname="Segoe UI"; fontsize=11; fontcolor="#a6adc8";
        style=dashed; color="#a6adc8"; bgcolor="#1e1e2e";

        skip_ws [label="WS -> skip" fillcolor="#45475a" fontcolor="#a6adc8"];
        skip_comment [label="COMMENT -> channel(HIDDEN)" fillcolor="#45475a" fontcolor="#a6adc8"];
      }

      grammar_decl -> options_block [style=bold];
      options_block -> pr_proto [style=bold label="  파서 영역  " fontcolor="#89b4fa"];
      pr_proto -> pr_syntax;
      pr_proto -> pr_topdef;
      pr_topdef -> pr_msgdef;
      pr_topdef -> pr_enumdef;
      pr_msgdef -> pr_field;
      pr_field -> pr_type;
      pr_field -> pr_ident;

      options_block -> lr_kw [style=bold label="  렉서 영역  " fontcolor="#f5c2e7"];
      lr_kw -> lr_sym [style=invis];
      lr_sym -> lr_lit [style=invis];
      lr_lit -> lr_id [style=invis];
      lr_id -> frag [label="  사용  " fontcolor="#a6adc8"];
      lr_lit -> frag [label="  사용  " fontcolor="#a6adc8"];
    }
  }`
};
