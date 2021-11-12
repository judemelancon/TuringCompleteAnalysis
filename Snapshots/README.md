# Turing Complete Analysis Snapshot
<!--IntroductionReplacementTarget See script for details. -->This was generated at 2021-11-12 16:52:31Z when there were 259,122 scores from 11,030 players.
Pull Requests to update this will be happily accepted at a reasonable pace.
Note that the images are links to larger images.

## Overall Levels Chart

The grey part is a simple area chart for the number of solvers.
There's also a logarithmic candlestick-style chart of sum scores;
 the full extent is the range of scores (excluding scores resulting from scoring issues), and the wider body connects the median and the mean.

Color|Meaning
-----|-------
![Green](https://via.placeholder.com/16/008000/008000)|Median < Mean
![Light Green](https://via.placeholder.com/16/90ee90/90ee90)| Median > Mean

<!-- Markdown usages like ![Level Chart](level chart.png) don't work for these images on GitHub;
      this is perhaps due to the image sizes.
     See https://github.com/judemelancon/TuringCompleteAnalysis/blob/b35b74977120d06bf4df7a2427bd74ec1675e085/Snapshots/README.md e.g.
     In any case, HTML <img> tags work fine on GitHub. -->
<img alt="Level Chart" width="900" src="level chart.png" />

## Level Details and Histograms

<!--LevelDetailsReplacementTarget See script for details. -->
Level|Solvers|in First|Best|Median|Mean|Histogram
-----|-------|--------|----|------|----|---------
crude_awakening|10,266|-|-|-|-|unscored
nand_gate|10,147|-|-|-|-|unscored
not_gate|9,817|9,050|3|3.0|3.191|<img alt="Histogram for not_gate" width="300" src="histogram not_gate.png" />
nor_gate|8,989|1,625|3|10.0|9.042|<img alt="Histogram for nor_gate" width="300" src="histogram nor_gate.png" />
or_gate|8,920|1,551|0|7.0|5.902|<img alt="Histogram for or_gate" width="300" src="histogram or_gate.png" />
and_gate|8,920|7,971|6|6.0|6.285|<img alt="Histogram for and_gate" width="300" src="histogram and_gate.png" />
always_on|8,740|8,739|0|0.0|0.000|<img alt="Histogram for always_on" width="300" src="histogram always_on.png" />
second_tick|8,176|262|6|9.0|10.89|<img alt="Histogram for second_tick" width="300" src="histogram second_tick.png" />
xor_gate|7,845|28|6|14.0|15.10|<img alt="Histogram for xor_gate" width="300" src="histogram xor_gate.png" />
and_gate_3|7,339|88|7|12.0|13.31|<img alt="Histogram for and_gate_3" width="300" src="histogram and_gate_3.png" />
or_gate_3|7,333|1,370|0|14.0|12.38|<img alt="Histogram for or_gate_3" width="300" src="histogram or_gate_3.png" />
dependency|6,987|-|-|-|-|unscored
xnor|6,982|84|6|17.0|18.30|<img alt="Histogram for xnor" width="300" src="histogram xnor.png" />
odd_number_of_signals|6,633|27|14|34.0|37.99|<img alt="Histogram for odd_number_of_signals" width="300" src="histogram odd_number_of_signals.png" />
any_doubles|6,263|19|7|39.0|36.20|<img alt="Histogram for any_doubles" width="300" src="histogram any_doubles.png" />
bit_adder|5,876|29|8|16.0|17.42|<img alt="Histogram for bit_adder" width="300" src="histogram bit_adder.png" />
full_adder|5,238|16|11|38.0|42.19|<img alt="Histogram for full_adder" width="300" src="histogram full_adder.png" />
counting_signals|5,236|7|16|74.0|83.17|<img alt="Histogram for counting_signals" width="300" src="histogram counting_signals.png" />
double_number|5,074|-|-|-|-|unscored
byte_not|4,717|4,362|10|10.0|10.64|<img alt="Histogram for byte_not" width="300" src="histogram byte_not.png" />
bit_inverter|4,710|22|6|14.0|15.92|<img alt="Histogram for bit_inverter" width="300" src="histogram bit_inverter.png" />
byte_or|4,709|1,018|0|28.0|21.76|<img alt="Histogram for byte_or" width="300" src="histogram byte_or.png" />
binary_racer|4,550|-|-|-|-|unscored
byte_adder|4,448|1|52|288.0|344.8|<img alt="Histogram for byte_adder" width="300" src="histogram byte_adder.png" />
saving_gracefully|4,326|2|8|15.0|17.98|<img alt="Histogram for saving_gracefully" width="300" src="histogram saving_gracefully.png" />
sr_latch|4,289|-|-|-|-|unscored
negative_numbers|4,266|-|-|-|-|unscored
byte_switch|4,256|10|18|20.0|23.08|<img alt="Histogram for byte_switch" width="300" src="histogram byte_switch.png" />
byte_mux|3,978|5|21|39.0|128.8|<img alt="Histogram for byte_mux" width="300" src="histogram byte_mux.png" />
signed_negator|3,937|1|9|330.0|389.7|<img alt="Histogram for signed_negator" width="300" src="histogram signed_negator.png" />
saving_bytes|3,770|2|40|84.0|127.5|<img alt="Histogram for saving_bytes" width="300" src="histogram saving_bytes.png" />
demux|3,718|3,205|3|3.0|5.194|<img alt="Histogram for demux" width="300" src="histogram demux.png" />
demux_3|3,560|47|15|54.0|75.69|<img alt="Histogram for demux_3" width="300" src="histogram demux_3.png" />
buffer|3,415|295|0|2.0|5.449|<img alt="Histogram for buffer" width="300" src="histogram buffer.png" />
ram_component|3,217|1|158|414.0|629.7|<img alt="Histogram for ram_component" width="300" src="histogram ram_component.png" />
tick_tock|3,207|1|57|473.0|569.0|<img alt="Histogram for tick_tock" width="300" src="histogram tick_tock.png" />
alu_1|3,090|2|41|193.0|335.9|<img alt="Histogram for alu_1" width="300" src="histogram alu_1.png" />
alu_2|2,921|1|122|1,025.0|1,272|<img alt="Histogram for alu_2" width="300" src="histogram alu_2.png" />
registers|2,587|-|-|-|-|unscored
decoder|2,440|1|9|45.0|62.29|<img alt="Histogram for decoder" width="300" src="histogram decoder.png" />
conditions|2,254|8|13|101.0|163.5|<img alt="Histogram for conditions" width="300" src="histogram conditions.png" />
computing_codes|2,194|-|-|-|-|unscored
program|2,131|-|-|-|-|unscored
constants|2,087|-|-|-|-|unscored
turing_complete|2,008|-|-|-|-|unscored
circumference|1,642|2|0|2,800.0|3,553|<img alt="Histogram for circumference" width="300" src="histogram circumference.png" />
mod_4|1,633|23|0|2,763.0|3,470|<img alt="Histogram for mod_4" width="300" src="histogram mod_4.png" />
spacial_invasion|1,570|-|-|-|-|unscored
byte_constant|1,216|937|0|0.0|1.622|<img alt="Histogram for byte_constant" width="300" src="histogram byte_constant.png" />
byte_and|1,212|752|20|20.0|34.57|<img alt="Histogram for byte_and" width="300" src="histogram byte_and.png" />
byte_equal|1,206|4|12|124.0|322.7|<img alt="Histogram for byte_equal" width="300" src="histogram byte_equal.png" />
component_factory|1,205|-|-|-|-|unscored
byte_xor|1,204|22|20|56.0|85.01|<img alt="Histogram for byte_xor" width="300" src="histogram byte_xor.png" />
compute_xor|1,107|1|54|2,743.0|3,370|<img alt="Histogram for compute_xor" width="300" src="histogram compute_xor.png" />
byte_less|1,095|1|26|343.0|495.7|<img alt="Histogram for byte_less" width="300" src="histogram byte_less.png" />
wide_instructions|1,076|-|-|-|-|unscored
leg_1|974|-|-|-|-|unscored
leg_2|959|-|-|-|-|unscored
leg_3|952|-|-|-|-|unscored
leg_4|933|-|-|-|-|unscored
delay_level|886|-|-|-|-|unscored
byte_less_i|729|1|26|498.0|619.7|<img alt="Histogram for byte_less_i" width="300" src="histogram byte_less_i.png" />
multiply|715|1|28|644.0|882.6|<img alt="Histogram for multiply" width="300" src="histogram multiply.png" />
stack|622|1|235|1,119.0|1,329|<img alt="Histogram for stack" width="300" src="histogram stack.png" />
push_pop|569|-|-|-|-|unscored
shift|517|1|33|3,784.0|4,858|<img alt="Histogram for shift" width="300" src="histogram shift.png" />
call_ret|504|-|-|-|-|unscored
unseen_fruit|348|-|-|-|-|unscored
test_lab|280|-|-|-|-|unscored
ai_showdown|231|4|5|5,013.5|6,141|<img alt="Histogram for ai_showdown" width="300" src="histogram ai_showdown.png" />
level_map|59|-|-|-|-|unscored
maze|38|1|364|6,992.0|7,391|<img alt="Histogram for maze" width="300" src="histogram maze.png" />
dance|18|1|84|510.0|2,655|<img alt="Histogram for dance" width="300" src="histogram dance.png" />
ram|15|1|2,854|4,301.0|4,857|<img alt="Histogram for ram" width="300" src="histogram ram.png" />
divide|12|1|662|3,301.0|4,060|<img alt="Histogram for divide" width="300" src="histogram divide.png" />
flood_predictor|11|1|1,439|3,888.0|4,357|<img alt="Histogram for flood_predictor" width="300" src="histogram flood_predictor.png" />
capitalize|9|1|2,117|5,126.0|5,460|<img alt="Histogram for capitalize" width="300" src="histogram capitalize.png" />
sorter|4|1|5,061|5,067.0|6,711|<img alt="Histogram for sorter" width="300" src="histogram sorter.png" />
tower|3|1|3,915|6,587.0|6,587|<img alt="Histogram for tower" width="300" src="histogram tower.png" />
robot_racing|2|1|4,527|6,863.0|6,863|<img alt="Histogram for robot_racing" width="300" src="histogram robot_racing.png" />
<!--/LevelDetailsReplacementTarget-->



