#include "simion.h"

#include "config.h"
#include "controller.h"
#include "actor-critic.h"
#include "q-learners.h"
#include "DQN.h"
#include "DDPG.h"
//#include "async-deep-simion.h"

std::shared_ptr<Simion> Simion::getInstance(ConfigNode* pConfigNode)
{

	return CHOICE<Simion>(pConfigNode, "Type", "The Simion class",
	{
		{"Controller", CHOICE_ELEMENT_FACTORY<Controller>},
		{"Actor-Critic", CHOICE_ELEMENT_NEW<ActorCritic>},
		{"Q-Learning", CHOICE_ELEMENT_NEW<QLearning>},
		{"Double-Q-Learning", CHOICE_ELEMENT_NEW<DoubleQLearning>},
		{"SARSA", CHOICE_ELEMENT_NEW<SARSA>},
		{"Inc-Natural-Actor-Critic", CHOICE_ELEMENT_NEW<IncrementalNaturalActorCritic>},
		{"Off-Policy-Actor-Critic", CHOICE_ELEMENT_NEW<OffPolicyActorCritic>}
#if defined(__linux__) || defined(_WIN64)
		,{"DQN", CHOICE_ELEMENT_NEW<DQN>}
		,{"Double-DQN", CHOICE_ELEMENT_NEW<DoubleDQN>}
		,{ "DDPG", CHOICE_ELEMENT_NEW<DDPG> }
#endif
		/*,{ "Async-QL-earning", CHOICE_ELEMENT_NEW<AsyncQLearning > }*/
	});
}

