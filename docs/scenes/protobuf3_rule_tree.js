// Scene plugin: D3.js interactive tree — Protobuf3.g4 rule hierarchy
// Type: d3tree | Section 6 of g4_EBNF_설명
// Usage: loaded by build_qna_html.js via loadScene('protobuf3_rule_tree')

module.exports = {
  type: 'd3tree',

  colors: {
    root:    { bg: '#e8f0fd', border: '#1a3a6e', label: 'Root (진입점)' },
    branch:  { bg: '#e8f0fd', border: '#2c5aa0', label: 'Parser Rule (분기)' },
    leaf:    { bg: '#fef9e7', border: '#b07d00', label: 'Parser Rule (말단)' },
    token:   { bg: '#f5e6ff', border: '#6a1b9a', label: 'Lexer Rule (토큰)' },
    wrapper: { bg: '#f0f0f0', border: '#999999', label: 'Wrapper Rule' }
  },

  trees: [
    {
      title: 'Protobuf3.g4 Parser Rule 계층',
      data: {
        name: 'proto', cat: 'root',
        children: [
          { name: 'syntax', cat: 'leaf' },
          { name: 'importStatement', cat: 'leaf' },
          { name: 'packageStatement', cat: 'leaf' },
          { name: 'optionStatement', cat: 'branch', children: [
            { name: 'optionName', cat: 'leaf' },
            { name: 'constant', cat: 'branch', children: [
              { name: 'fullIdent', cat: 'leaf' },
              { name: 'intLit', cat: 'leaf' },
              { name: 'floatLit', cat: 'leaf' },
              { name: 'strLit', cat: 'leaf' },
              { name: 'boolLit', cat: 'leaf' }
            ]}
          ]},
          { name: 'topLevelDef', cat: 'branch', children: [
            { name: 'messageDef', cat: 'branch', children: [
              { name: 'messageName', cat: 'wrapper' },
              { name: 'messageBody', cat: 'branch', children: [
                { name: 'field', cat: 'branch', children: [
                  { name: 'fieldLabel', cat: 'leaf' },
                  { name: 'type_', cat: 'branch', children: [
                    { name: 'DOUBLE, INT32...', cat: 'token' },
                    { name: 'messageType', cat: 'leaf' },
                    { name: 'enumType', cat: 'leaf' }
                  ]},
                  { name: 'fieldName', cat: 'wrapper' },
                  { name: 'fieldNumber', cat: 'wrapper' },
                  { name: 'fieldOptions', cat: 'leaf' }
                ]},
                { name: 'enumDef', cat: 'branch', children: [
                  { name: 'enumName', cat: 'wrapper' },
                  { name: 'enumBody', cat: 'branch', children: [
                    { name: 'enumField', cat: 'leaf' },
                    { name: 'reserved', cat: 'leaf' }
                  ]}
                ]},
                { name: 'oneof', cat: 'branch', children: [
                  { name: 'oneofName', cat: 'wrapper' },
                  { name: 'oneofField', cat: 'leaf' }
                ]},
                { name: 'mapField', cat: 'branch', children: [
                  { name: 'keyType', cat: 'leaf' },
                  { name: 'mapName', cat: 'wrapper' }
                ]},
                { name: 'reserved', cat: 'branch', children: [
                  { name: 'ranges', cat: 'leaf' },
                  { name: 'reservedFieldNames', cat: 'leaf' }
                ]}
              ]}
            ]},
            { name: 'enumDef', cat: 'branch' },
            { name: 'extendDef', cat: 'leaf' },
            { name: 'serviceDef', cat: 'branch', children: [
              { name: 'serviceName', cat: 'wrapper' },
              { name: 'rpc', cat: 'leaf' }
            ]}
          ]}
        ]
      }
    }
  ],

  html: function(ids) {
    var legendItems = Object.keys(this.colors).map(function(key) {
      var c = this.colors[key];
      return '<span style="display:inline-flex;align-items:center;gap:4px;margin-right:12px;">' +
        '<span style="display:inline-block;width:14px;height:14px;border-radius:3px;' +
        'background:' + c.bg + ';border:2px solid ' + c.border + ';"></span>' +
        '<span style="font-size:0.82em;color:#555;">' + c.label + '</span></span>';
    }.bind(this)).join('');

    var panelId = ids.panel0;
    var tipId = ids.tip0;
    return '<div id="' + ids.wrapper + '" class="d3tree-wrapper">' +
      '<div style="font-weight:bold;text-align:center;margin-bottom:6px;color:#2c5aa0;">' +
        this.trees[0].title + '</div>' +
      '<div id="' + panelId + '" class="d3tree-container" style="min-height:500px;"></div>' +
      '<div id="' + tipId + '" class="d3tree-tip"></div>' +
      '<div id="' + ids.legend + '" class="d3tree-legend">' + legendItems + '</div>' +
      '</div>';
  },

  build: function(ids) {
    var colorsJson = JSON.stringify(this.colors);
    var dataJson = JSON.stringify(this.trees[0].data);
    return 'var colors = ' + colorsJson + ';\n' +
      'D3Tree.render(' +
        JSON.stringify(ids.panel0) + ', ' +
        JSON.stringify(ids.tip0) + ', ' +
        dataJson + ', colors);';
  }
};
